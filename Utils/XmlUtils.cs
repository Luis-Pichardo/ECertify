using System.Xml.Serialization;

namespace eCertify.Utils
{
    public static class XmlUtils
    {
        public static T DeserializarXml<T>(string xmlContent)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xmlContent);
            return (T)serializer.Deserialize(reader);
        }
    }
}
