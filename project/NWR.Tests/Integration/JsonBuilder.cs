using System;
using System.Globalization;
using System.Text;

namespace NWR.Tests.Integration
{
    /// <summary>
    /// Minimal JSON writer for catalog dumps (no third-party JSON on .NET 4.5.2 Mono).
    /// </summary>
    public sealed class JsonBuilder
    {
        private readonly StringBuilder fSb = new StringBuilder();
        private bool fNeedComma;

        public void BeginObject()
        {
            Comma();
            fSb.Append('{');
            fNeedComma = false;
        }

        public void EndObject()
        {
            fSb.Append('}');
            fNeedComma = true;
        }

        public void BeginArray()
        {
            Comma();
            fSb.Append('[');
            fNeedComma = false;
        }

        public void EndArray()
        {
            fSb.Append(']');
            fNeedComma = true;
        }

        public void Key(string name)
        {
            Comma();
            WriteString(name);
            fSb.Append(':');
            fNeedComma = false;
        }

        public void Value(string value)
        {
            Comma();
            if (value == null) {
                fSb.Append("null");
            } else {
                WriteString(value);
            }
            fNeedComma = true;
        }

        public void Value(int value)
        {
            Comma();
            fSb.Append(value.ToString(CultureInfo.InvariantCulture));
            fNeedComma = true;
        }

        public void Value(bool value)
        {
            Comma();
            fSb.Append(value ? "true" : "false");
            fNeedComma = true;
        }

        public void Value(float value)
        {
            Comma();
            fSb.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            fNeedComma = true;
        }

        public void Property(string name, string value)
        {
            Key(name);
            Value(value);
        }

        public void Property(string name, int value)
        {
            Key(name);
            Value(value);
        }

        public void Property(string name, bool value)
        {
            Key(name);
            Value(value);
        }

        public void Property(string name, float value)
        {
            Key(name);
            Value(value);
        }

        public override string ToString()
        {
            return fSb.ToString();
        }

        private void Comma()
        {
            if (fNeedComma) {
                fSb.Append(',');
            }
        }

        private void WriteString(string s)
        {
            fSb.Append('"');
            for (int i = 0; i < s.Length; i++) {
                char c = s[i];
                switch (c) {
                    case '\\':
                        fSb.Append("\\\\");
                        break;
                    case '"':
                        fSb.Append("\\\"");
                        break;
                    case '\n':
                        fSb.Append("\\n");
                        break;
                    case '\r':
                        fSb.Append("\\r");
                        break;
                    case '\t':
                        fSb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20) {
                            fSb.AppendFormat("\\u{0:x4}", (int)c);
                        } else {
                            fSb.Append(c);
                        }
                        break;
                }
            }
            fSb.Append('"');
        }
    }
}
