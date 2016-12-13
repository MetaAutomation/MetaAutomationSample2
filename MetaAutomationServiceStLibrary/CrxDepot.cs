////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MetaAutomation (C) 2016 by Matt Griscom.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace MetaAutomationServiceStLibrary
{
    using MetaAutomationBaseStLibrary;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;

    /// <summary>
    /// This class manages short-term storage and validation of the XML for the CheckRunLaunch (CRL) objects and the CheckRunArtifact (CRA) objects.
    /// This class implements the Singleton pattern.
    /// </summary>
    internal class CrxDepot
    {
        #region publicMethods
        public static CrxDepot Instance
        {
            get
            {
                lock(m_TypeLockObject)
                {
                    if (CrxDepot.m_TheInstance == null)
                    {
                        CrxDepot.m_TheInstance = new CrxDepot();
                    }
                }

                return CrxDepot.m_TheInstance;
            }
        }

        public string SetCrlXDocument(XDocument crlXDoc)
        {
            string uniqueLabelForCheckRunSegment = CheckRunDataHandles.CreateIdFromCrxXDocument(crlXDoc);
            string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

            string crlFileName = CrlFilePrefix + this.FileNameFromTruncatedId(truncatedId);
            string fullFileNameAndPath = Path.Combine(this.m_CrlDirectory, crlFileName);
            crlXDoc.Save(fullFileNameAndPath);

            return uniqueLabelForCheckRunSegment;
        }

        public string SetCraXDocument(XDocument xDocCra)
        {
            string uniqueLabelForCheckRunSegment = CheckRunDataHandles.CreateIdFromCrxXDocument(xDocCra);
            string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

            string craFileName = CraFilePrefix + this.FileNameFromTruncatedId(truncatedId);
            string fullFileNameAndPath = Path.Combine(this.m_CraDirectory, craFileName);
            xDocCra.Save(fullFileNameAndPath);

            return uniqueLabelForCheckRunSegment;
        }

        public XDocument GetCraXDocument(string uniqueLabelForCheckRunSegment)
        {
            XDocument result = null;
            string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

            string craFileName = CraFilePrefix + this.FileNameFromTruncatedId(truncatedId);
            result = XDocument.Load(Path.Combine(this.m_CraDirectory, craFileName));

            return result;
        }

        public XDocument GetCrlXDocument(string uniqueLabelForCheckRunSegment)
        {
            XDocument result = null;
            string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

            string crlFileName = CrlFilePrefix + this.FileNameFromTruncatedId(truncatedId);
            result = XDocument.Load(Path.Combine(this.m_CrlDirectory, crlFileName));

            return result;
        }

        public bool ClearCrlXDocumentById(string uniqueLabelForCheckRunSegment)
        {
            bool documentRemoved = false;

            try
            {
                string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

                lock (m_CheckRunLaunchLockObject)
                {
                    documentRemoved = m_CRL_XDocuments.Remove(truncatedId);
                }
            }
            catch (KeyNotFoundException)
            {
                // do nothing
            }

            return documentRemoved;
        }

        public bool ClearCraXDocumentById(string uniqueLabelForCheckRunSegment)
        {
            bool documentRemoved = false;

            try
            {
                string truncatedId = CheckRunDataHandles.StripIdOfMachineNames(uniqueLabelForCheckRunSegment);

                lock (m_CheckRunArtifactLockObject)
                {
                    documentRemoved = m_CRA_XDocuments.Remove(truncatedId);
                }
            }
            catch (KeyNotFoundException)
            {
                // do nothing
            }

            return documentRemoved;
        }

#endregion //publicMethods
#region privateMembers
        private static object m_TypeLockObject = new Object();
        private static CrxDepot m_TheInstance = null;

        private Dictionary<string, XDocument> m_CRA_XDocuments = null;
        private object m_CheckRunArtifactLockObject = null;

        private Dictionary<string, XDocument> m_CRL_XDocuments = null;
        private object m_CheckRunLaunchLockObject = null;

        private static int m_instanceCounter = 0;
#endregion
#region privateMethods

        private CrxDepot()
        {
            const string FileIntraProcessCommunicationPathRoot = @"..\..\Artifacts\temp";
            const string CheckRunArtifactDirectory = "CRA";
            const string CheckRunLaunchDirectory = "CRL";
            string intraProcessCommunicationPathRoot = Path.Combine(Environment.CurrentDirectory, FileIntraProcessCommunicationPathRoot);
            intraProcessCommunicationPathRoot = Path.GetFullPath(intraProcessCommunicationPathRoot);

            m_CRA_XDocuments = new Dictionary<string, XDocument>();
            m_CheckRunArtifactLockObject = new Object();
            m_CRL_XDocuments = new Dictionary<string, XDocument>();
            m_CheckRunLaunchLockObject = new Object();
            m_instanceCounter++;

            // Initialize file set
            if (!Directory.Exists(intraProcessCommunicationPathRoot))
            {
                // Create base data directory
                Directory.CreateDirectory(intraProcessCommunicationPathRoot);
            }

            DirectoryInfo pathToNormalize = new DirectoryInfo(Path.Combine(intraProcessCommunicationPathRoot, CheckRunArtifactDirectory));

            this.m_CraDirectory = Path.Combine(intraProcessCommunicationPathRoot, CheckRunArtifactDirectory);
            this.InitializeDirectory(this.m_CraDirectory);

            this.m_CrlDirectory = Path.Combine(intraProcessCommunicationPathRoot, CheckRunLaunchDirectory);
            this.InitializeDirectory(m_CrlDirectory);
        }

        private string ListCraKeys()
        {
            int count = m_CRA_XDocuments.Count;
            StringBuilder keys = new StringBuilder(string.Format("total count:{0}", count));

            foreach (KeyValuePair<string, XDocument> kvp in m_CRA_XDocuments)
            {
                keys.Append(Environment.NewLine);
                keys.Append(kvp.Key);
            }

            keys.Append(Environment.NewLine);

            return keys.ToString();
        }

        private string ListCrlKeys()
        {
            int count = m_CRL_XDocuments.Count;
            StringBuilder keys = new StringBuilder(string.Format("total count:{0}", count));

            foreach (KeyValuePair<string, XDocument> kvp in m_CRL_XDocuments)
            {
                keys.Append(Environment.NewLine);
                keys.Append(kvp.Key);
            }

            keys.Append(Environment.NewLine);

            return keys.ToString();
        }

#endregion //privateMethods
        private void InitializeDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                // Clean out the Directory because we don't need the old files
                foreach (string fileName in Directory.EnumerateFiles(path))
                {
                    string fullPathAndName = Path.Combine(path, fileName);
                    DateTime fileCreationTime = File.GetCreationTimeUtc(fullPathAndName);
                    TimeSpan fileAge = DateTime.UtcNow - fileCreationTime;

                    if (fileAge > TimeSpan.FromHours(1.0))
                    {
                        // get rid of files more than an hour old
                        File.Delete(fullPathAndName);
                    }
                }
            }
            else
            {
                // Create new Directory to hold data
                Directory.CreateDirectory(path);
            }
        }

        private string FileNameFromTruncatedId(string id)
        {
            string result = id;
            int cutPoint = id.LastIndexOf(Path.DirectorySeparatorChar);

            if (cutPoint > 0)
            {
                // Assume there is string beyond separator
                result = result.Substring(cutPoint + 1) + ".xml";
            }

            return result;
        }

        private string m_CraDirectory = null;
        private string m_CrlDirectory = null;
        const string CraFilePrefix = "CRA_";
        const string CrlFilePrefix = "CRL_";
    }
}
