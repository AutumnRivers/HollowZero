using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Linq;

namespace HollowZero
{
    public static class XDocumentExtensions
    {
        public static XElement GetRequiredChild(this XElement baseElement, string elementName)
        {
            if(baseElement.Elements().ToList().TryFind(c => c.Name == elementName, out var childElement))
            {
                return childElement;
            } else
            {
                throw new FormatException($"<{baseElement.Name}> is missing required child element: <{elementName}>");
            }
        }

        public static string ReadRequiredAttribute(this XElement element, string attributeName)
        {
            FormatException missingAttributeException = new FormatException($"<{element.Name}> is missing required attribute: \"{attributeName}\"");

            if(element.Attributes().ToList().TryFind(a => a.Name == attributeName, out var attr))
            {
                if(attr.Value.IsNullOrWhiteSpace())
                {
                    throw missingAttributeException;
                }
                return attr.Value;
            } else
            {
                throw missingAttributeException;
            }
        }
    }

    public class MissingContentException : Exception
    {
        public MissingContentException() : base() { }

        public MissingContentException(string message) : base(message) { }

        public MissingContentException(string message, Exception inner) : base(message, inner) { }

        public MissingContentException(XElement element) :
            base($"<{element.Name} /> needs content, but it is empty.") { }
    }
}
