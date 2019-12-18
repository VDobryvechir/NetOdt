﻿using NetCoreOdt.Enumerations;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace NetCoreOdt
{
    /// <summary>
    /// Class to create and write ODT documents
    /// </summary>
    public sealed class OdtDocument : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// The full file path (directory + name for the ODT file)
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The temporary working folder, will delete when <see cref="Dispose"/> is called
        /// </summary>
        public string TempWorkingPath { get; private set; }

        /// <summary>
        /// The count of the tables
        /// </summary>
        public int TableCount { get; private set; }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// The XML content of the content file
        /// </summary>
        internal XmlDocument ContentFile { get; private set; }

        /// <summary>
        /// The raw content before the style content
        /// </summary>
        internal StringBuilder BeforeStyleContent { get; private set; }

        /// <summary>
        /// The raw style content
        /// </summary>
        internal StringBuilder StyleContent { get; private set; }

        /// <summary>
        /// The raw content after the style content and before the text content
        /// </summary>
        internal StringBuilder AfterStyleContent { get; private set; }

        /// <summary>
        /// The raw content after the text content
        /// </summary>
        internal StringBuilder TextContent { get; private set; }

        /// <summary>
        /// The raw text content
        /// </summary>
        internal StringBuilder AfterTextContent { get; private set; }

        /// <summary>
        /// The path to the content file (typical inside the <see cref="TempWorkingPath"/>)
        /// </summary>
        internal string ContentFilePath { get; private set; }

        #endregion Internal Properties

        #region Public Constructors

        /// <summary>
        /// Create a new ODT document, save the ODT document as "Unknown.odt" and use a automatic generated temporary folder
        /// under the <see cref="Environment.SpecialFolder.LocalApplicationData"/> folder
        /// </summary>
        public OdtDocument()
            : this("Unknown.odt")
        {
        }

        /// <summary>
        /// Create a new ODT document, save the ODT document into the given file path and use a automatic generated temporary folder
        /// under the <see cref="Environment.SpecialFolder.LocalApplicationData"/> folder
        /// </summary>
        /// <param name="filePath">The save path for the ODT document</param>
        public OdtDocument(in string filePath)
            : this(filePath,
                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetCoreOdt", Guid.NewGuid().ToString()))
        {
        }

        /// <summary>
        /// Create a new ODT document, save the ODT document into the given file path and use the given temporary folder
        /// </summary>
        /// <param name="filePath">The save path for the ODT document</param>
        /// <param name="tempWorkingPath">The temporary working path for the none zipped document files</param>
        public OdtDocument(in string filePath, in string tempWorkingPath)
        {
            TableCount         = 0;

            FilePath           = filePath;
            TempWorkingPath    = tempWorkingPath;

            ContentFilePath    = Path.Combine(TempWorkingPath, "content.xml");

            ContentFile        = new XmlDocument();

            BeforeStyleContent = new StringBuilder();
            StyleContent       = new StringBuilder();
            AfterStyleContent  = new StringBuilder();
            TextContent        = new StringBuilder();
            AfterTextContent    = new StringBuilder();

            CreateOdtTemplate();
            ReadContent();
        }

        #endregion Public Constructors

        #region Public Methods - Write Table

        /// <summary>
        /// Write an empty unformatted table with the given row and cell count into the document
        /// </summary>
        /// <param name="rowCount">The count of the rows</param>
        /// <param name="columnCount">The count of the columns</param>
        public void WriteTable(in int rowCount, in int columnCount)
        {
            TableCount++;

            TextContent.Append($"<table:table table:name=\"Tabelle{TableCount}\" table:style-name=\"Tabelle1\">");
            TextContent.Append($"<table:table-column table:style-name=\"Tabelle1.A\" table:number-columns-repeated=\"{columnCount}\"/>");

            for(var rowNumber = 1; rowNumber <= rowCount; rowNumber++)
            {
                TextContent.Append("<table:table-row>");

                for(var columnNumber = 1; columnNumber <= columnCount; columnNumber++)
                {
                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }

        /// <summary>
        /// Write a unformatted table and fill it with the given data from the <see cref="DataTable"/>
        /// </summary>
        /// <param name="dataTable">The <see cref="DataTable"/> that contains the data for the table</param>
        public void WriteTable(in DataTable dataTable)
        {

            TableCount++;

            TextContent.Append($"<table:table table:name=\"Tabelle{TableCount}\" table:style-name=\"Tabelle1\">");
            TextContent.Append($"<table:table-column table:style-name=\"Tabelle1.A\" table:number-columns-repeated=\"{dataTable.Columns.Count}\"/>");

            var rowNumber = 0;

            foreach(DataRow? dataRow in dataTable.Rows)
            {
                rowNumber++;

                if(dataRow is null)
                {
                    continue;
                }

                TextContent.Append("<table:table-row>");

                var columnCount  = dataRow.ItemArray.Length;
                var columnNumber = 0;

                foreach(var column in dataRow.ItemArray)
                {
                    columnNumber++;

                    if(column is null)
                    {
                        continue;
                    }

                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    Write(column.ToString() ?? string.Empty, TextStyle.Normal);
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }

        #endregion Public Methods - Write Table

        #region Public Methods - Write Text

        /// <summary>
        /// Write a single line with a unformatted value to the document
        /// </summary>
        /// <param name="value">The value to write into the document</param>
        public void Write(in ValueType value)
            => Write(value, TextStyle.Normal);

        /// <summary>
        /// Write a single line with a unformatted text to the document (Note: line breaks "\n" will currently not working)
        /// </summary>
        /// <param name="text">The text to write into the document</param>
        public void Write(in string text)
            => Write(text, TextStyle.Normal);

        /// <summary>
        /// Write the content of the given <see cref="StringBuilder"/> as unformatted text into the document (Note: line breaks "\n" will currently not working)
        /// </summary>
        /// <param name="content">The <see cref="StringBuilder"/> that contains the content for the document</param>
        public void Write(in StringBuilder content)
            => Write(content, TextStyle.Normal);

        /// <summary>
        /// Write a single line with a styled value to the document
        /// </summary>
        /// <param name="value">The value to write into the document</param>
        /// <param name="style">The text style of the value</param>
        public void Write(in ValueType value, in TextStyle style)
        {
            TextContent.Append($"<text:p text:style-name=\"{GetStyleName(style)}\">");
            TextContent.Append(value);
            TextContent.Append("</text:p>");
        }

        /// <summary>
        /// Write a single line with a styled text to the document (Note: line breaks "\n" will currently not working)
        /// </summary>
        /// <param name="text">The text to write into the document</param>
        /// <param name="style">The text style of the text</param>
        public void Write(in string text, in TextStyle style)
        {
            TextContent.Append($"<text:p text:style-name=\"{GetStyleName(style)}\">");
            TextContent.Append(text);
            TextContent.Append("</text:p>");
        }

        /// <summary>
        /// Write the content of the given <see cref="StringBuilder"/> as styled text the document (Note: line breaks "\n" will currently not working)
        /// </summary>
        /// <param name="content">The <see cref="StringBuilder"/> that contains the content for the document</param>
        /// <param name="style">The text style of the content</param>
        public void Write(in StringBuilder content, in TextStyle style)
        {
            TextContent.Append($"<text:p text:style-name=\"{GetStyleName(style)}\">");
            TextContent.Append(content);
            TextContent.Append("</text:p>");
        }

        #endregion Public Methods - Write Text

        #region Public Methods - Save

        /// <summary>
        /// Save the change content and create the ODT document into the given path and automatic override a existing file
        /// </summary>
        /// <param name="filePath">The save path for the ODT document</param>
        public void SaveAs(in string filePath)
        {
            FilePath = filePath;

            Save(overrideExistingFile: true);
        }

        /// <summary>
        /// Save the change content and create the ODT document into the given path
        /// </summary>
        /// <param name="filePath">The save path for the ODT document</param>
        /// <param name="overrideExistingFile">Indicate that a existing file will be override</param>
        public void SaveAs(in string filePath, in bool overrideExistingFile)
        {
            FilePath = filePath;

            Save(overrideExistingFile);
        }

        /// <summary>
        /// Save the change content and create the ODT document and automatic override a existing file
        /// </summary>
        public void Save()
            => Save(overrideExistingFile: true);

        /// <summary>
        /// Save the change content and create the ODT document
        /// </summary>
        /// <param name="overrideExistingFile">Indicate that a existing file will be override</param>
        public void Save(in bool overrideExistingFile)
        {
            WriteContent();

            if(overrideExistingFile && File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            ZipFile.CreateFromDirectory(TempWorkingPath, FilePath);
        }

        #endregion Public Methods - Save

        #region Public Methods - Dispose

        /// <summary>
        /// Save the document (override when existing), delete the <see cref="TempWorkingPath"/> folder and free all resources
        /// </summary>
        public void Dispose()
            => Dispose(overrideExistingFile: true);

        /// <summary>
        /// Save the document , delete the <see cref="TempWorkingPath"/> folder and free all resources
        /// </summary>
        public void Dispose(in bool overrideExistingFile)
        {
            Save(overrideExistingFile);

            Directory.Delete(TempWorkingPath, true);

            BeforeStyleContent.Clear();
            StyleContent.Clear();
            AfterStyleContent.Clear();
            TextContent.Clear();
            AfterTextContent.Clear();

            ContentFile     = new XmlDocument();
            TempWorkingPath = string.Empty;
            ContentFilePath = string.Empty;
        }

        #endregion Public Methods - Dispose

        #region Internal Methods

        /// <summary>
        /// Create a folder with a minimum of files that are need by a ODT file
        /// </summary>
        internal void CreateOdtTemplate()
        {
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if(assemblyFolder is null)
            {
                throw new DirectoryNotFoundException($"Assembly directory [{assemblyFolder}] not found");
            }

            var originalFolder = Path.Combine(assemblyFolder, "Original");

            Directory.CreateDirectory(Path.Combine(TempWorkingPath, "Configurations2"));
            Directory.CreateDirectory(Path.Combine(TempWorkingPath, "META-INF"));
            Directory.CreateDirectory(Path.Combine(TempWorkingPath, "Thumbnails"));

            foreach(var file in Directory.GetFiles(originalFolder))
            {
                File.Copy(file, Path.Combine(TempWorkingPath, Path.GetFileName(file)), true);
            }

            // Important: respect the uppercase letters in the folder name
            File.Copy(Path.Combine(originalFolder, "META-INF", "manifest.xml"), Path.Combine(TempWorkingPath, "META-INF", "manifest.xml"), true);

            // Important: respect the uppercase letter in the folder name
            File.Copy(Path.Combine(originalFolder, "Thumbnails", "thumbnail.png"), Path.Combine(TempWorkingPath, "Thumbnails", "thumbnail.png"), true);
        }

        /// <summary>
        /// Read the complete content for the content file
        /// </summary>
        internal void ReadContent()
        {
            ContentFile.Load(ContentFilePath);

            // don't use short using syntax to avoid not closed and disposed stream

            using(var fileStream = File.OpenRead(ContentFilePath))
            {

                using(var textReader = new StreamReader(fileStream))
                {
                    var rawFileContent    = textReader.ReadToEnd();
                    var textContentSplit  = rawFileContent.Split("<text:p text:style-name=\"Standard\"/>");
                    var styleContentSplit = textContentSplit[0].Split("<office:automatic-styles/>");

                    BeforeStyleContent.Append(styleContentSplit.ElementAtOrDefault(0) ?? string.Empty);
                    AfterStyleContent.Append(styleContentSplit.ElementAtOrDefault(1) ?? string.Empty);
                    AfterTextContent.Append(textContentSplit.ElementAtOrDefault(1) ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Add all needed styles for all <see cref="TextStyle"/> combinations to the style content
        /// </summary>
        internal void AddStandardStyles()
        {
            // P1 - Normal - 000
            StyleContent.Append("<style:style style:name=\"P1\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties/></style:style>");

            // P2 - Bold - 001
            StyleContent.Append("<style:style style:name=\"P2\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties fo:font-weight=\"bold\" style:font-weight-asian=\"bold\" style:font-weight-complex=\"bold\"/></style:style>");

            // P3 - Italic -010
            StyleContent.Append("<style:style style:name=\"P3\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties fo:font-style=\"italic\" style:font-style-asian=\"italic\" style:font-style-complex=\"italic\"/></style:style>");

            // P4 - Bold + Italic - 011
            StyleContent.Append("<style:style style:name=\"P4\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties fo:font-style=\"italic\" fo:font-weight=\"bold\" style:font-style-asian=\"italic\" style:font-weight-asian=\"bold\" style:font-style-complex=\"italic\" style:font-weight-complex=\"bold\"/></style:style>");

            // P5 0b_100 - Underline
            StyleContent.Append("<style:style style:name=\"P5\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties style:text-underline-style=\"solid\" style:text-underline-width=\"auto\" style:text-underline-color=\"font-color\"/></style:style>");

            // P6 - 0b_101 - Bold + Underline
            StyleContent.Append("<style:style style:name=\"P6\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties style:text-underline-style=\"solid\" style:text-underline-width=\"auto\" style:text-underline-color=\"font-color\" fo:font-weight=\"bold\" style:font-weight-asian=\"bold\" style:font-weight-complex=\"bold\"/></style:style>");

            // P7 - 0b_110 - Italic + Underline
            StyleContent.Append("<style:style style:name=\"P7\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties fo:font-style=\"italic\" style:text-underline-style=\"solid\" style:text-underline-width=\"auto\" style:text-underline-color=\"font-color\" style:font-style-asian=\"italic\" style:font-style-complex=\"italic\"/></style:style>");

            // P8 - 0b_111 - Bold + Italic + Underline
            StyleContent.Append("<style:style style:name=\"P8\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:text-properties fo:font-style=\"italic\" style:text-underline-style=\"solid\" style:text-underline-width=\"auto\" style:text-underline-color=\"font-color\" fo:font-weight=\"bold\" style:font-style-asian=\"italic\" style:font-weight-asian=\"bold\" style:font-style-complex=\"italic\" style:font-weight-complex=\"bold\"/></style:style>");
        }

        /// <summary>
        /// Add all needed styles for simple tables
        /// </summary>
        internal void AddTableStyles()
        {
            if(TableCount < 1)
            {
                // When a document has no tables, we don't need a table style
                return;
            }

            StyleContent.Append("<style:style style:name=\"Tabelle1\" style:family=\"table\">");
            StyleContent.Append("<style:table-properties style:width=\"17cm\" table:align=\"margins\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"Tabelle1.A\" style:family=\"table-column\">");
            StyleContent.Append("<style:table-column-properties style:column-width=\"3.401cm\" style:rel-column-width=\"13107*\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"Tabelle1.A1\" style:family=\"table-cell\">");
            StyleContent.Append("<style:table-cell-properties fo:padding=\"0.097cm\" fo:border-left=\"0.05pt solid #000000\" fo:border-right=\"none\" fo:border-top=\"0.05pt solid #000000\" fo:border-bottom=\"0.05pt solid #000000\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"Tabelle1.E1\" style:family=\"table-cell\"><style:table-cell-properties fo:padding=\"0.097cm\" fo:border=\"0.05pt solid #000000\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"Tabelle1.A2\" style:family=\"table-cell\"><style:table-cell-properties fo:padding=\"0.097cm\" fo:border-left=\"0.05pt solid #000000\" fo:border-right=\"none\" fo:border-top=\"none\" fo:border-bottom=\"0.05pt solid #000000\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"Tabelle1.E2\" style:family=\"table-cell\"><style:table-cell-properties fo:padding=\"0.097cm\" fo:border-left=\"0.05pt solid #000000\" fo:border-right=\"0.05pt solid #000000\" fo:border-top=\"none\" fo:border-bottom=\"0.05pt solid #000000\"/>");
            StyleContent.Append("</style:style>");
            StyleContent.Append("<style:style style:name=\"P1\" style:family=\"paragraph\" style:parent-style-name=\"Table_20_Contents\">");
            StyleContent.Append("</style:style>");
        }

        /// <summary>
        /// Write the complete content to the content file (overwrite the existing file)
        /// </summary>
        internal void WriteContent()
        {
            AddStandardStyles();
            AddTableStyles();

            // don't use short using syntax to avoid not closed and disposed stream

            using(var fileStream = File.Create(ContentFilePath))
            {
                using(var textWriter = new StreamWriter(fileStream))
                {
                    textWriter.Write(BeforeStyleContent);
                    textWriter.Write("<office:automatic-styles>");
                    textWriter.Write(StyleContent);
                    textWriter.Write("</office:automatic-styles>");
                    textWriter.Write(AfterStyleContent);
                    textWriter.Write(TextContent);
                    textWriter.Write(AfterTextContent);
                }
            }
        }

        /// <summary>
        /// Return the name representation of a given style or style combination
        /// </summary>
        /// <param name="style">The style for the style name</param>
        /// <returns>The name representation of the style or style combination</returns>
        internal string GetStyleName(in TextStyle style)
            => style switch
            {
                TextStyle.Normal                                        => "P1",
                TextStyle.Bold                                          => "P2",
                TextStyle.Italic                                        => "P3",
                TextStyle.Bold | TextStyle.Italic                       => "P4",
                TextStyle.Underline                                     => "P5",
                TextStyle.Bold | TextStyle.Underline                    => "P6",
                TextStyle.Italic | TextStyle.Underline                  => "P7",
                TextStyle.Bold | TextStyle.Italic | TextStyle.Underline => "P8",

                _ => throw new NotSupportedException("Style combination has no style entry")
            };

        /// <summary>
        /// Return the name representation for a style for the given table cell
        /// </summary>
        /// <param name="rowNumber">The row number of the current cell</param>
        /// <param name="columnNumber">The column number of the current cell</param>
        /// <param name="columnCount">The column count of the current row</param>
        /// <returns>The name representation of a table column</returns>
        internal string GetTableCellStyleName(in int rowNumber, in int columnNumber, in int columnCount)
        {
            var number = rowNumber == 1 ? "1" : "2";

            var prefix = columnCount == columnNumber ? "E" : "A";

            return prefix + number;
        }

        #endregion Internal Methods
    }
}
