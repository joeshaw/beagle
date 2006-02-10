//
// Mono.Data.SqliteClient.SqliteDataReader.cs
//
// Provides a means of reading a forward-only stream of rows from a Sqlite 
// database file.
//
// Author(s): Vladimir Vukicevic  <vladimir@pobox.com>
//            Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//			  Joshua Tauberer <tauberer@for.net>
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
		private string[] columns;
		private Hashtable column_names_sens, column_names_insens;
		private bool closed;
		private string[] decltypes;
		private int[] declmode;

		
		#endregion

		#region Constructors and destructors
		
		internal SqliteDataReader (SqliteCommand cmd, IntPtr _pVm, int _version)
		{
			command = cmd;
			pVm = _pVm;
			version = _version;

			current_row = new ArrayList();

			column_names_sens = new Hashtable ();
			column_names_insens = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
			closed = false;
		}
		
		#endregion

		#region Properties
		
		public int Depth {
			get { return 0; }
		}
		
		public int FieldCount {
			get { return columns.Length; }
		}
		
		public object this[string name] {
			get {
				return GetValue (GetOrdinal (name));
			}
		}
		
		public object this[int i] {
			get { return GetValue (i); }
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
			int pN;
			IntPtr pazValue;
			IntPtr pazColName;
			bool first = true;
			
			bool hasdata = command.ExecuteStatement(pVm, out pN, out pazValue, out pazColName);

			// For the first row, get the column information (names and types)
			if (columns == null) {
				if (version == 3) {
					// A decltype might be null if the type is unknown to sqlite.
					decltypes = new string[pN];
					declmode = new int[pN]; // 1 == integer, 2 == datetime
					for (int i = 0; i < pN; i++) {
						IntPtr decl = Sqlite.sqlite3_column_decltype16 (pVm, i);
						if (decl != IntPtr.Zero) {
							decltypes[i] = Marshal.PtrToStringUni (decl).ToLower(System.Globalization.CultureInfo.InvariantCulture);
							if (decltypes[i] == "int" || decltypes[i] == "integer")
								declmode[i] = 1;
							else if (decltypes[i] == "date" || decltypes[i] == "datetime")
								declmode[i] = 2;
						}
					}
				}
					
				columns = new string[pN];	
				for (int i = 0; i < pN; i++) {
					string colName;
					if (version == 2) {
						IntPtr fieldPtr = Marshal.ReadIntPtr (pazColName, i*IntPtr.Size);
						colName = Sqlite.HeapToString (fieldPtr, command.Connection.Encoding);
					} else {
						colName = Marshal.PtrToStringUni (Sqlite.sqlite3_column_name16 (pVm, i));
					}
					columns[i] = colName;
					column_names_sens [colName] = i;
					column_names_insens [colName] = i;
				}
			}

			if (!hasdata)
				return false;
				
			current_row.Clear();
			for (int i = 0; i < pN; i++) {
				if (version == 2) {
					IntPtr fieldPtr = Marshal.ReadIntPtr (pazValue, i*IntPtr.Size);
					current_row.Add (Sqlite.HeapToString (fieldPtr, command.Connection.Encoding));
				} else {
					switch (Sqlite.sqlite3_column_type (pVm, i)) {
						case 1:
							long val = Sqlite.sqlite3_column_int64 (pVm, i);
							
							// If the column was declared as an 'int' or 'integer', let's play
							// nice and return an int (version 3 only).
							if (declmode[i] == 1 && val >= int.MinValue && val <= int.MaxValue)
								current_row.Add ((int)val);
								
								// Or if it was declared a date or datetime, do the reverse of what we
								// do for DateTime parameters.
							else if (declmode[i] == 2)
								current_row.Add (DateTime.FromFileTime(val));
								
							else
								current_row.Add (val);
									
							break;
						case 2:
							current_row.Add (Sqlite.sqlite3_column_double (pVm, i));
							break;
						case 3:
							string strval = Marshal.PtrToStringUni (Sqlite.sqlite3_column_text16 (pVm, i));
							current_row.Add (Marshal.PtrToStringUni (Sqlite.sqlite3_column_text16 (pVm, i)));
								
							// If the column was declared as a 'date' or 'datetime', let's play
							// nice and return a DateTime (version 3 only).
							if (declmode[i] == 2)
								current_row.Add (DateTime.Parse (strval));
							else
								current_row.Add (strval);

							break;
						case 4:
							int blobbytes = Sqlite.sqlite3_column_bytes16 (pVm, i);
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
					Sqlite.sqlite3_finalize (pVm);
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
			return NextResult ();
		}

		#endregion
		
		#region IDataRecord getters
		
		public bool GetBoolean (int i)
		{
			return Convert.ToBoolean (current_row[i]);
		}
		
		public byte GetByte (int i)
		{
			return Convert.ToByte (current_row[i]);
		}
		
		public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException ();
		}
		
		public char GetChar (int i)
		{
			return Convert.ToChar (current_row[i]);
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
			if (decltypes != null && decltypes[i] != null)
				return decltypes[i];
			return "text"; // SQL Lite data type
		}
		
		public DateTime GetDateTime (int i)
		{
			return Convert.ToDateTime (current_row[i]);
		}
		
		public decimal GetDecimal (int i)
		{
			return Convert.ToDecimal (current_row[i]);
		}
		
		public double GetDouble (int i)
		{
			return Convert.ToDouble (current_row[i]);
		}
		
		public Type GetFieldType (int i)
		{
			if (current_row == null)
				return null;

			return current_row [i].GetType ();
		}
		
		public float GetFloat (int i)
		{
			return Convert.ToSingle (current_row[i]);
		}
		
		public Guid GetGuid (int i)
		{
			throw new NotImplementedException ();
		}
		
		public short GetInt16 (int i)
		{
			return Convert.ToInt16 (current_row[i]);
		}
		
		public int GetInt32 (int i)
		{
			return Convert.ToInt32 (current_row[i]);
		}
		
		public long GetInt64 (int i)
		{
			return Convert.ToInt64 (current_row[i]);
		}
		
		public string GetName (int i)
		{
			return columns[i];
		}
		
		public int GetOrdinal (string name)
		{
			object v = column_names_sens[name];
			if (v == null)
				v = column_names_insens[name];
			if (v == null)
				throw new ArgumentException("Column does not exist.");
			return (int) v;
		}
		
		public string GetString (int i)
		{
			return current_row[i].ToString();
		}
		
		public object GetValue (int i)
		{
			return current_row[i];
		}
		
		public int GetValues (object[] values)
		{
			int num_to_fill = System.Math.Min (values.Length, columns.Length);
			for (int i = 0; i < num_to_fill; i++) {
				if (current_row[i] != null) {
					values[i] = current_row[i];
				} else {
					values[i] = DBNull.Value;
				}
			}
			return num_to_fill;
		}
		
		public bool IsDBNull (int i)
		{
			return (current_row[i] == null);
		}
		        
		#endregion
	}
}
