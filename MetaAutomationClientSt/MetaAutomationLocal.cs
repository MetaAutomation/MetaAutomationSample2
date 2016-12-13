////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MetaAutomation (C) 2016 by Matt Griscom.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace MetaAutomationClientSt
{
    using MetaAutomationServiceStLibrary;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public class MetaAutomationLocal
    {
        const string MetaAutomationLocalDefaultMessage = "The local call has completed.";

        public string StartCheckRun(string checkRunLaunchXml)
        {
            string result = MetaAutomationBaseStLibrary.DataStringConstants.StatusString.DefaultServiceSuccessMessage;

            if (checkRunLaunchXml == null) throw new CheckInfrastructureServiceException("StartCheckRun: the passed string argument is null.");
            if (checkRunLaunchXml.Length == 0) throw new CheckInfrastructureServiceException("StartCheckRun: the passed string argument is zero-length.");

            XDocument crlXDocument = CheckRunValidation.Instance.ValidateCheckRunLaunchIntoXDocument(checkRunLaunchXml);

            CheckDestinationServices.Instance.StartCheckRun(crlXDocument);

            return result;
        }

        public string CompleteCheckRun(string checkRunArtifactXml)
        {
            string result = MetaAutomationBaseStLibrary.DataStringConstants.StatusString.DefaultServiceSuccessMessage;

            if (checkRunArtifactXml == null) throw new CheckInfrastructureServiceException("CompleteCheckRun: the passed string argument is null.");
            if (checkRunArtifactXml.Length == 0) throw new CheckInfrastructureServiceException("CompleteCheckRun: the passed string argument is zero-length.");

            XDocument craXDocument = CheckRunValidation.Instance.ValidateCheckRunArtifactIntoXDocument(checkRunArtifactXml);

            CheckOriginServices.Instance.CompleteCheckRun(craXDocument);

            return result;
        }

        public string GetCheckRunArtifact(string uniqueLabelForCheckRunSegment)
        {
            if (uniqueLabelForCheckRunSegment == null) throw new CheckInfrastructureServiceException("GetCheckRunArtifact: the passed string argument is null.");
            if (uniqueLabelForCheckRunSegment.Length == 0) throw new CheckInfrastructureServiceException("GetCheckRunArtifact: the passed string argument is zero-length.");

            return CheckOriginServices.Instance.GetCheckRunArtifact(uniqueLabelForCheckRunSegment);
        }

        public string GetCheckRunLaunch(string uniqueLabelForCheckRunSegment)
        {
            if (uniqueLabelForCheckRunSegment == null) throw new CheckInfrastructureServiceException("GetCheckRunLaunch: the passed string argument is null.");
            if (uniqueLabelForCheckRunSegment.Length == 0) throw new CheckInfrastructureServiceException("GetCheckRunLaunch: the passed string argument is zero-length.");

            return CheckDestinationServices.Instance.GetCheckRunLaunch(uniqueLabelForCheckRunSegment);
        }

        public string AbortCheckRun(string uniqueLabelForCheckRunSegment, string errorMessage)
        {
            string result = string.Empty;

            if (uniqueLabelForCheckRunSegment == null) throw new CheckInfrastructureServiceException("AbortCheckRun: the passed string argument 'uniqueLabelForCheckRunSegment' is null.");
            if (uniqueLabelForCheckRunSegment.Length == 0) throw new CheckInfrastructureServiceException("AbortCheckRun: the passed string argument 'uniqueLabelForCheckRunSegment' is zero-length.");
            if (errorMessage == null) throw new CheckInfrastructureServiceException("AbortCheckRun: the passed string argument 'errorMessage' is null.");
            if (errorMessage.Length == 0) throw new CheckInfrastructureServiceException("AbortCheckRun: the passed string argument 'errorMessage' is zero-length.");

            try
            {
                result += CheckDestinationServices.Instance.AbortCheckRun(uniqueLabelForCheckRunSegment, errorMessage);
            }
            catch (Exception)
            {
                // do nothing
            }
            try
            {
                result += CheckOriginServices.Instance.AbortCheckRun(uniqueLabelForCheckRunSegment, errorMessage);
            }
            catch (Exception)
            {
                // do nothing
            }

            return result;
        }

        public string GetAbortMessage(string uniqueLabelForCheckRunSegment)
        {
            string result = string.Empty;

            if (uniqueLabelForCheckRunSegment == null) throw new CheckInfrastructureServiceException("GetAbortMessage: the passed string argument 'uniqueLabelForCheckRunSegment' is null.");
            if (uniqueLabelForCheckRunSegment.Length == 0) throw new CheckInfrastructureServiceException("GetAbortMessage: the passed string argument 'uniqueLabelForCheckRunSegment' is zero-length.");

            try
            {
                result += CheckOriginServices.Instance.GetAbortMessage(uniqueLabelForCheckRunSegment);
            }
            catch (Exception)
            {
                // do nothing
            }

            return result;
        }
    }
}
