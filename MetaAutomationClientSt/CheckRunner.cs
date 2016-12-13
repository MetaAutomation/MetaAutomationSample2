////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MetaAutomation (C) 2016 by Matt Griscom.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace MetaAutomationClientSt
{
    using MetaAutomationBaseStLibrary;
    using MetaAutomationClientStLibrary;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// Manages checks from CheckRunLaunch (CRL) to CheckRunArtifact (CRA)
    /// </summary>
    public class CheckRunner
    {
        public CheckRunner()
        {
            m_MetaAutomationLocal = new MetaAutomationLocal();
        }

        /// <summary>
        /// Runs a check to completion, or throws an exception
        /// </summary>
        /// <param name="checkRunLaunch">The check run launch (CRL) object</param>
        /// <returns>The check run artifact (CRA)</returns>
        public XDocument Run(XDocument checkRunLaunch)
        {
            // This identifier is used for the named semaphore that synchronizes for the cross-process, cross-machine check run
            string uniqueLabelForCheckRunSegment = CheckRunDataHandles.CreateIdFromCrxXDocument(checkRunLaunch);
            string semaphoreServiceSideUser = Environment.UserName;
            string semaphoreTimeoutString = DataAccessors.GetCheckRunValue(checkRunLaunch, DataStringConstants.NameAttributeValues.SemaphoreTimeoutMilliseconds);

            int semaphoreTimeoutMS = 0;

            try
            {
                semaphoreTimeoutMS = int.Parse(semaphoreTimeoutString);
            }
            catch (Exception ex)
            {
                throw new CheckInfrastructureClientException(string.Format("Parse failed for the configured SemaphoreTimeoutMilliseconds string '{0}'. See InnerException", semaphoreTimeoutString), ex);
            }

            // Create semaphore with modification rights for the test user
            Semaphore checkRunSemaphore = MetaAutomationClientStLibrary.Synchronization.CreateAndGetNamedSemaphore(semaphoreServiceSideUser, uniqueLabelForCheckRunSegment);

            string startResult = m_MetaAutomationLocal.StartCheckRun(checkRunLaunch.ToString());


            if (startResult != MetaAutomationBaseStLibrary.DataStringConstants.StatusString.DefaultServiceSuccessMessage)
            {
                throw new CheckInfrastructureClientException(string.Format("StartCheckRun failed with error '{0}'", startResult));
            }

            bool receivedSignal = checkRunSemaphore.WaitOne(semaphoreTimeoutMS);

            string resultString = null;
            XDocument resultCRA = null;

            try
            {
                if (receivedSignal)
                {
                    resultString = m_MetaAutomationLocal.GetCheckRunArtifact(uniqueLabelForCheckRunSegment);

                    resultCRA = DataValidation.Instance.ValidateCheckRunArtifactIntoXDocument(resultString);
                }
                else
                {
                    throw new CheckRunException(string.Format("A semaphore waiting on completion of a sub-check timed out at the configured '{0}' milliseconds.", semaphoreTimeoutMS));
                }
            }
            catch (CheckInfrastructureClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string abortMessage = m_MetaAutomationLocal.GetAbortMessage(uniqueLabelForCheckRunSegment);

                throw new CheckInfrastructureClientException(string.Format("Check run failed with error '{0}'. The abort message is '{1}'. See InnerException.", resultString, abortMessage), ex);
            }

            return resultCRA;
        }

        // on server side: id -> CRL -> CRA -> returns through service
        // loads assembly, finds check method, invokes it
        public void Run(string[] argumentsFromServiceLaunch)
        {
            string uniqueLabelForCheckRunSegment = null;

            try
            {
                if ((argumentsFromServiceLaunch == null) || (argumentsFromServiceLaunch.Length == 0))
                {
                    throw new CheckRunException("Zero arguments received by Run method on the destination side. One is required: the CRI string to identify the check segment.");
                }

                uniqueLabelForCheckRunSegment = argumentsFromServiceLaunch[0];
                string checkRunLaunchString = m_MetaAutomationLocal.GetCheckRunLaunch(uniqueLabelForCheckRunSegment);

                XDocument checkRunLaunch = DataValidation.Instance.ValidateCheckRunLaunchIntoXDocument(checkRunLaunchString);
                string targetCheckMethodGuid = DataAccessors.GetCheckRunValue(checkRunLaunch, DataStringConstants.NameAttributeValues.CheckMethodGuid);
                string checkAssemblyName = DataAccessors.GetCheckRunValue(checkRunLaunch, DataStringConstants.NameAttributeValues.CheckLibraryAssembly);
                Assembly checkAssembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, checkAssemblyName));

                Type targetType = null;
                MethodInfo targetMethod = null;
                string methodNameGivenInAttribute = string.Empty;

                this.GetMethodAndType(targetCheckMethodGuid, checkAssembly.GetTypes(), out targetMethod, out targetType, out methodNameGivenInAttribute);

                if ((targetMethod == null) || (targetType == null))
                {
                    string pathToLaunch = DataAccessors.GetCheckRunValue(checkRunLaunch, DataStringConstants.NameAttributeValues.PathAndFileToRunner);
                    throw new CheckInfrastructureClientException(string.Format("The check method was not found. File path='{0}', current directory='{1}', check assembly='{2}', target type='{3}', method GUID='{4}'.", pathToLaunch, Environment.CurrentDirectory ,checkAssemblyName, targetType, targetCheckMethodGuid));
                }

                // Create instance
                object testObject = Activator.CreateInstance(targetType);

                // Initialize MetaAutomation client lib
                CheckArtifact checkArtifact = Check.CheckArtifactInstance;
                checkArtifact.InitializeCheckRunFromCheckRunLaunch(checkRunLaunch, new CheckConstants.RunSubCheckDelegate(Run));

                string methodStepName = string.Format("Method {0}", methodNameGivenInAttribute);
                try
                {
                    checkArtifact.DoStep(methodStepName, delegate
                    {
                        // Run the test method synchronously
                        targetMethod.Invoke(testObject, null);
                    });
                }
                catch (Exception ex)
                {
                    checkArtifact.AddCheckExceptionInformation(ex);
                }

                XDocument craXdoc = checkArtifact.CompleteCheckRun();
                m_MetaAutomationLocal.CompleteCheckRun(craXdoc.ToString());

            }
            catch (Exception ex)
            {
                if (uniqueLabelForCheckRunSegment != null)
                {
                    m_MetaAutomationLocal.AbortCheckRun(uniqueLabelForCheckRunSegment, ex.ToString());
                    throw new CheckInfrastructureClientException("Check was aborted.", ex);
                }
                else
                {
                    throw ex;
                }
            }
        }

        private MetaAutomationLocal m_MetaAutomationLocal = null;

        private void GetMethodAndType(string targetCheckMethodGuid, Type[] typesInAssembly, out MethodInfo methodInfoOut, out Type targetTypeOut, out string methodNameFromAttribute)
        {
            methodInfoOut = null;
            targetTypeOut = null;
            methodNameFromAttribute = null;

            foreach (Type type in typesInAssembly)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (MethodInfo method in methods)
                {
                    Attribute attribute = method.GetCustomAttribute(typeof(CheckMethodAttribute));

                    if (attribute != null)
                    {
                        if (attribute is CheckMethodAttribute)
                        {
                            CheckMethodAttribute checkMethodAttribute = (CheckMethodAttribute)attribute;
                            string checkMethodGuid = checkMethodAttribute.CheckMethodGuid;

                            if (targetCheckMethodGuid == checkMethodGuid)
                            {
                                methodInfoOut = method;
                                targetTypeOut = type;
                                methodNameFromAttribute = checkMethodAttribute.CheckMethodName;
                                break;
                            }
                        }
                    }
                }

                if (methodInfoOut != null)
                {
                    break;
                }
            }
        }
    }
}
