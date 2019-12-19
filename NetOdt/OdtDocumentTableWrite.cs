﻿using NetCoreOdt.Enumerations;
using NetCoreOdt.Helper;
using System;
using System.Data;
using System.Text;

namespace NetCoreOdt
{
    /// <summary>
    /// Class to create and write ODT documents
    /// </summary>
    public sealed partial class OdtDocument : IDisposable
    {
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
                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{StyleHelper.GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }

        /// <summary>
        /// Write an unformatted table with the given row and cell count into the document and fill each cell with the given value
        /// </summary>
        /// <param name="rowCount">The count of the rows</param>
        /// <param name="columnCount">The count of the columns</param>
        /// <param name="value">The value for each cell</param>
        public void WriteTable(in int rowCount, in int columnCount, in ValueType value)
        {
            TableCount++;

            TextContent.Append($"<table:table table:name=\"Tabelle{TableCount}\" table:style-name=\"Tabelle1\">");
            TextContent.Append($"<table:table-column table:style-name=\"Tabelle1.A\" table:number-columns-repeated=\"{columnCount}\"/>");

            for(var rowNumber = 1; rowNumber <= rowCount; rowNumber++)
            {
                TextContent.Append("<table:table-row>");

                for(var columnNumber = 1; columnNumber <= columnCount; columnNumber++)
                {
                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{StyleHelper.GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    Write(value);
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }

        /// <summary>
        /// Write an unformatted table with the given row and cell count into the document and fill each cell with the given text
        /// </summary>
        /// <param name="rowCount">The count of the rows</param>
        /// <param name="columnCount">The count of the columns</param>
        /// <param name="text">The text for each cell</param>
        public void WriteTable(in int rowCount, in int columnCount, in string text)
        {
            TableCount++;

            TextContent.Append($"<table:table table:name=\"Tabelle{TableCount}\" table:style-name=\"Tabelle1\">");
            TextContent.Append($"<table:table-column table:style-name=\"Tabelle1.A\" table:number-columns-repeated=\"{columnCount}\"/>");

            for(var rowNumber = 1; rowNumber <= rowCount; rowNumber++)
            {
                TextContent.Append("<table:table-row>");

                for(var columnNumber = 1; columnNumber <= columnCount; columnNumber++)
                {
                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{StyleHelper.GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    Write(text);
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }

        /// <summary>
        /// Write an unformatted table with the given row and cell count into the document and fill each cell with the given content
        /// </summary>
        /// <param name="rowCount">The count of the rows</param>
        /// <param name="columnCount">The count of the columns</param>
        /// <param name="content">The content for each cell</param>
        public void WriteTable(in int rowCount, in int columnCount, in StringBuilder content)
        {
            TableCount++;

            TextContent.Append($"<table:table table:name=\"Tabelle{TableCount}\" table:style-name=\"Tabelle1\">");
            TextContent.Append($"<table:table-column table:style-name=\"Tabelle1.A\" table:number-columns-repeated=\"{columnCount}\"/>");

            for(var rowNumber = 1; rowNumber <= rowCount; rowNumber++)
            {
                TextContent.Append("<table:table-row>");

                for(var columnNumber = 1; columnNumber <= columnCount; columnNumber++)
                {
                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{StyleHelper.GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    Write(content);
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

                    TextContent.Append($"<table:table-cell table:style-name=\"Tabelle1.{StyleHelper.GetTableCellStyleName(rowNumber, columnNumber, columnCount)}\" office:value-type=\"string\">");
                    Write(column.ToString() ?? string.Empty, TextStyle.Normal);
                    TextContent.Append($"</table:table-cell>");
                }

                TextContent.Append("</table:table-row>");
            }

            TextContent.Append("</table:table>");
        }
    }
}