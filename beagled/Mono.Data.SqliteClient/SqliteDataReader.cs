//
// Mono.Data.SqliteClient.SqliteDataReader.cs
//
// Provides a means of reading a forward-only stream of rows from a Sqlite 
// database file.
//
// Author(s): Vladimir Vukicevic  <vladimir@pobox.com>
//            Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//
// Copyright (C) 2002  Vladimir Vukicevic
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace Mono.Data.SqliteClient
{
	public class SqliteDataReader : MarshalByRefObject, IEnumerable, IDataReader, IDisposable, IDataRecord
	{

		#region Fields
		
		private SqliteCommand command;
		private IntPtr pVm;
		private int version;

		private ArrayList current_row;

		private ArrayList columns;
		private Hashtable column_names;
		private bool closed;
		
		#endregion

		#region Constructors and destructors
		
		internal SqliteDataReader (SqliteCommand cmd, IntPtr _pVm, int _version)
		{
			command = cmd;
			pVm = _pVm;
			version = _version;

			current_row = new ArrayList ();

			columns = new ArrayList ();
			column_names = new Hashtable ();
			closed = false;
		}
		
		#endregion

		#region Properties
		
		public int Depth {
			get { return 0; }
		}
		
		public int FieldCount {
			get { return columns.Count; }
		}
		
		public object this[string name] {
			get { return current_row [(int) column_names[name]]; }
		}
		
		public object this[int i] {
			get { return current_row [i]; }
		}
		
		public bool IsClosed {
			get { return closed; }
		}
		
		public int RecordsAffected {
			get { return command.NumChanges (); }
		}
		
		#endregion

		#region Internal Methods

		internal bool ReadNextColumn ()
		{
			int pN = 0;
			IntPtr pazValue = IntPtr.Zero;
			IntPtr pazColName = IntPtr.Zero;
			SqliteError res;

			if (version == 3) {
				res = Sqlite.sqlite3_step (pVm);
				pN = Sqlite.sqlite3_column_count (pVm);
			} else
				res = Sqlite.sqlite_step (pVm, out pN, out pazValue, out pazColName);

			if (res == SqliteError.DONE) {
				return false;
			}

			if (res != SqliteError.ROW)
				throw new SqliteException (res);

			// We have some data; lets read it

			// If we are reading the first column, populate the column names
			if (column_names.Count == 0) {
				for (int i = 0; i < pN; i++) {
					string colName = "";
					if (version == 2) {
						IntPtr fieldPtr = (IntPtr)Marshal.ReadInt32 (pazColName, i*IntPtr.Size);
						colName = Marshal.PtrToStringAnsi (fieldPtr);
					} else {
						colName = Marshal.PtrToStringAnsi (Sqlite.sqlite3_column_name (pVm, i));
					}
					columns.Add (colName);
					column_names [colName] = i;
				}
			}

			// Now read the actual data
			current_row.Clear ();
			for (int i = 0; i < pN; i++) {
				string colData = "";
				if (version == 2) {
					IntPtr fieldPtr = (IntPtr)Marshal.ReadInt32 (pazValue, i*IntPtr.Size);
					colData = Marshal.PtrToStringAnsi (fieldPtr);
					current_row.Add (Marshal.PtrToStringAnsi (fieldPtr));
				} else {
					switch (Sqlite.sqlite3_column_type (pVm, i)) {
					case 1:
						Int64 sqliteint64 = Sqlite.sqlite3_column_int64 (pVm, i);
						current_row.Add (sqliteint64.ToString ());
						break;
					case 2:
						double sqlitedouble = Sqlite.sqlite3_column_double (pVm, i);
						current_row.Add (sqlitedouble.ToString ());
						break;
					case 3:
						colData = Marshal.PtrToStringAnsi (Sqlite.sqlite3_column_text (pVm, i));
						current_row.Add (colData);
						break;
					case 4:
						int blobbytes = Sqlite.sqlite3_column_bytes (pVm, i);
						IntPtr blobptr = Sqlite.sqlite3_column_blob (pVm, i);
						byte[] blob = new byte[blobbytes];
						Marshal.Copy (blobptr, blob, 0, blobbytes);
						current_row.Add (blob);
						break;
					case 5:
						current_row.Add (null);
						break;
					default:
						throw new ApplicationException ("FATAL: Unknown sqlite3_column_type");
					}
				}	
			}
			
			return true;
		}
		
		#endregion

		#region  Public Methods
		
		public void Close ()
		{
			closed = true;

			if (pVm != IntPtr.Zero) {
				IntPtr errMsg;
				if (version == 3)
					Sqlite.sqlite3_finalize (pVm, out errMsg);
				else
					Sqlite.sqlite_finalize (pVm, out errMsg);
				pVm = IntPtr.Zero;
			}
		}
		
		public void Dispose ()
		{
			Close ();
		}
		
		IEnumerator IEnumerable.GetEnumerator () 
		{
			return new DbEnumerator (this);
		}
		
		public DataTable GetSchemaTable () 
		{
			DataTable dataTableSchema = new DataTable ();
			
			dataTableSchema.Columns.Add ("ColumnName", typeof (String));
			dataTableSchema.Columns.Add ("ColumnOrdinal", typeof (Int32));
			dataTableSchema.Columns.Add ("ColumnSize", typeof (Int32));
			dataTableSchema.Columns.Add ("NumericPrecision", typeof (Int32));
			dataTableSchema.Columns.Add ("NumericScale", typeof (Int32));
			dataTableSchema.Columns.Add ("IsUnique", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsKey", typeof (Boolean));
			dataTableSchema.Columns.Add ("BaseCatalogName", typeof (String));
			dataTableSchema.Columns.Add ("BaseColumnName", typeof (String));
			dataTableSchema.Columns.Add ("BaseSchemaName", typeof (String));
			dataTableSchema.Columns.Add ("BaseTableName", typeof (String));
			dataTableSchema.Columns.Add ("DataType", typeof(Type));
			dataTableSchema.Columns.Add ("AllowDBNull", typeof (Boolean));
			dataTableSchema.Columns.Add ("ProviderType", typeof (Int32));
			dataTableSchema.Columns.Add ("IsAliased", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsExpression", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsIdentity", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsAutoIncrement", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsRowVersion", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsHidden", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsLong", typeof (Boolean));
			dataTableSchema.Columns.Add ("IsReadOnly", typeof (Boolean));
			
			dataTableSchema.BeginLoadData();
			for (int i = 0; i < this.FieldCount; i += 1 ) {
				
				DataRow schemaRow = dataTableSchema.NewRow ();
				
				schemaRow["ColumnName"] = columns[i];
				schemaRow["ColumnOrdinal"] = i;
				schemaRow["ColumnSize"] = 0;
				schemaRow["NumericPrecision"] = 0;
				schemaRow["NumericScale"] = 0;
				schemaRow["IsUnique"] = false;
				schemaRow["IsKey"] = false;
				schemaRow["BaseCatalogName"] = "";
				schemaRow["BaseColumnName"] = columns[i];
				schemaRow["BaseSchemaName"] = "";
				schemaRow["BaseTableName"] = "";
				schemaRow["DataType"] = typeof(string);
				schemaRow["AllowDBNull"] = true;
				schemaRow["ProviderType"] = 0;
				schemaRow["IsAliased"] = false;
				schemaRow["IsExpression"] = false;
				schemaRow["IsIdentity"] = false;
				schemaRow["IsAutoIncrement"] = false;
				schemaRow["IsRowVersion"] = false;
				schemaRow["IsHidden"] = false;
				schemaRow["IsLong"] = false;
				schemaRow["IsReadOnly"] = false;
				
				dataTableSchema.Rows.Add (schemaRow);
				schemaRow.AcceptChanges();
			}
			dataTableSchema.EndLoadData();
			
			return dataTableSchema;
		}
		
		public bool NextResult ()
		{
			return ReadNextColumn ();
		}
		
		public bool Read ()
		{
			return ReadNextColumn ();
		}

		#endregion
		
		#region IDataRecord getters
		
		public bool GetBoolean (int i)
		{
			return Convert.ToBoolean ((string) current_row [i]);
		}
		
		public byte GetByte (int i)
		{
			return Convert.ToByte ((string) current_row [i]);
		}
		
		public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException ();
		}
		
		public char GetChar (int i)
		{
			return Convert.ToChar ((string) current_row [i]);
		}
		
		public long GetChars (int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException ();
		}
		
		public IDataReader GetData (int i)
		{
			throw new NotImplementedException ();
		}
		
		public string GetDataTypeName (int i)
		{
			return "text"; // SQL Lite data type
		}
		
		public DateTime GetDateTime (int i)
		{
			return Convert.ToDateTime ((string) current_row [i]);
		}
		
		public decimal GetDecimal (int i)
		{
			return Convert.ToDecimal ((string) current_row [i]);
		}
		
		public double GetDouble (int i)
		{
			return Convert.ToDouble ((string) current_row [i]);
		}
		
		public Type GetFieldType (int i)
		{
			return System.Type.GetType ("System.String"); // .NET data type
		}
		
		public float GetFloat (int i)
		{
			return Convert.ToSingle ((string) current_row [i]);
		}
		
		public Guid GetGuid (int i)
		{
			throw new NotImplementedException ();
		}
		
		public short GetInt16 (int i)
		{
			return Convert.ToInt16 ((string) current_row [i]);
		}
		
		public int GetInt32 (int i)
		{
			return Convert.ToInt32 ((string) current_row [i]);
		}
		
		public long GetInt64 (int i)
		{
			return Convert.ToInt64 ((string) current_row [i]);
		}
		
		public string GetName (int i)
		{
			return (string) columns[i];
		}
		
		public int GetOrdinal (string name)
		{
			return (int) column_names[name];
		}
		
		public string GetString (int i)
		{
			return (string) current_row [i];
		}
		
		public object GetValue (int i)
		{
			return current_row [i];
		}
		
		public int GetValues (object[] values)
		{
			int num_to_fill = System.Math.Min (values.Length, columns.Count);
			for (int i = 0; i < num_to_fill; i++) {
				if (current_row [i] != null) {
					values[i] = current_row [i];
				} else {
					values[i] = DBNull.Value;
				}
			}
			return num_to_fill;
		}
		
		public bool IsDBNull (int i)
		{
			return current_row [i] == null;
		}
		        
		#endregion
	}
}
