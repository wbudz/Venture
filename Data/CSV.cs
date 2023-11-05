using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Budziszewski.Venture.Data
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

        public IEnumerable<T> Interpret<T>() where T : DataPoint, new()
        {
            if (Lines == null) yield break;
            if (Headers == null) yield break;

            var headers = Headers.Select(x => x.ToLower()).ToArray();

            T newItem;

            for (int i = 0; i < Lines.Length; i++)
            {
                try
                {
                    newItem = new();
                    newItem.FromCSV(headers, Lines[i], i);
                    if (!newItem.Active) continue;
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
    }
}
