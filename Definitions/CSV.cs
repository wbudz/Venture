using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Venture.Modules;

namespace Venture
{
    public class CSV
    {
        public string Path { get; private set; }

        public string Name { get; private set; }

        public string[]? Headers { get; private set; }

        public string[][]? Lines { get; private set; }

        public CSV(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void Read()
        {
            if (!(File.Exists(Path)))
            {
                throw new IOException("File not exists.");
            }

            char delimiter = ';';

            string? line;
            using (StreamReader sr = new StreamReader(new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                try
                {
                    if ((line = sr.ReadLine()) != null)
                    {
                        delimiter = GuessDelimiter(line);
                    }
                }
                catch
                {
                    throw new Exception("File is empty.");
                }
            }

            List<string[]> lines = new List<string[]>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(Path))
            {
                try
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(delimiter.ToString());
                    // headers
                    if (!parser.EndOfData)
                    {
                        //Processing row
                        Headers = parser.ReadFields();
                    }
                    // cells
                    while (!parser.EndOfData)
                    {
                        //Processing row
                        string[]? fields = parser.ReadFields();
                        if (fields != null) lines.Add(fields);
                    }
                }
                catch
                {
                    throw new Exception("Error reading file.");
                }
            }

            Lines = lines.ToArray();
        }

        public IEnumerable<Dictionary<string, string>> Interpret()
        {
            if (Lines == null) yield break;
            if (Headers == null) yield break;

            var headers = Headers.Select(x => x.ToLower()).ToArray();

            for (int i = 0; i < Lines.Length; i++)
            {
                Dictionary<string, string> newItem = new();
                try
                {
                    for (int j = 0; j < headers.Length; j++)
                    {
                        newItem.Add(headers[j], Lines[i][j]);
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException($"Error interpreting CSV file (line: {Lines[i].Aggregate((a, b) => a + ";" + b)}; exception: {ex}).");
                }

                yield return newItem;
            }
        }

        private static char GuessDelimiter(string line)
        {
            char d = ';';
            bool insideQuote = false;

            // if there is unquoted semicolon, it is preferred as a delimiter
            // otherwise, tab character or colon are presumed to be delimiters, if they are present
            // semicolon is default delimiter if no character is found 

            foreach (char c in line)
            {
                if (c == '"') insideQuote = !insideQuote;
                if (insideQuote) continue;
                if (c == ';') { d = ';'; return d; }
                if (c == '\t') { d = '\t'; }
                if (c == ',') d = ',';
            }

            return d;
        }

        public static string Export<T>(IEnumerable<T> list)
        {
            StringBuilder sb = new StringBuilder();

            if (typeof(T) is IEnumerable)
            {
                foreach (var line in list)
                {
                    sb.AppendJoin('\t', line);
                }
                sb.AppendLine();
            }
            else
            {
                var properties = typeof(T).GetProperties();
                sb.AppendJoin('\t', properties.Select(x => x.Name));
                sb.AppendLine();
                foreach (var line in list)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (i > 0) sb.Append('\t');
                        object? value = properties[i].GetValue(line);
                        if (value == null) continue;
                        if (value is System.Collections.IEnumerable && !(value is string)) continue;
                        sb.Append(properties[i].GetValue(line));
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
