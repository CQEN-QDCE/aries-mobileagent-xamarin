using System;

namespace Osma.Mobile.App.Utilities
{
    public class SchemaId
    {
        private SchemaId()
        {
        }

        public SchemaId(string did, string name, string version)
        {
            Did = did;
            Marker = 2;
            Name = name;
            Version = version;
        }

        public string Did { get; private set; }

        public int Marker { get; private set; }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public static SchemaId Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException();

            string[] parts = value.Split(':');

            if (parts.Length == 8)
            {
                // schema:sov:did:sov:NcYxiDXkpYi6ov5FcYDi1e:2:gvt:1.0
                // did = did:sov:NcYxiDXkpYi6ov5FcYDi1e Fully qualified
            }

            if (parts.Length == 4)
            {
                // NcYxiDXkpYi6ov5FcYDi1e:2:gvt:1.0
            }

            int marker;
            if (!int.TryParse(parts[1], out marker)) throw new FormatException();
            if (marker != 2) throw new FormatException();

            var schemaId = new SchemaId();

            schemaId.Did = parts[0];
            schemaId.Name = parts[2];
            schemaId.Version = parts[3];

            return schemaId;
        }

        public override string ToString()
        {
            return Did + ":" + Marker + ":" + Name + ":" + Version;
        }
    }
}