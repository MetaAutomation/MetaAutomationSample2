////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MetaAutomation (C) 2016 by Matt Griscom.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace MetaAutomationServiceStLibrary
{
    using System.Xml.Linq;

    public interface ICheckOriginServices
    {
        string CompleteCheckRun(XDocument checkRunArtifact);
		string GetCheckRunArtifact(string uniqueLabelForCheckRunSegment);
        string AbortCheckRun(string uniqueLabelForCheckRunSegment, string errorMessage);
        string GetAbortMessage(string uniqueLabelForCheckRunSegment);
    }
}
