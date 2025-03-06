using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using static Caspar.Api;

namespace Caspar.Database
{
    public class ResultSet : IEnumerable<ResultSet.Cursor>
    {


        public bool HasCursors
        {
            get
            {
                if (cursors.Count == 0) { return false; }
                return true;
            }
        }

        public Cursor this[int index]
        {
            get
            {
                if (cursors.Count == 0) { return Singleton<Cursor>.Instance; }
                if (index >= cursors.Count) { return Singleton<Cursor>.Instance; }
                return cursors[index] as Cursor;
            }
        }

        public void Merge(ResultSet rhs)
        {
            foreach (var c in rhs)
            {
                cursors.Add(c);
            }
        }

        public Cursor AddCursor()
        {
            Cursor cursor = new Cursor();
            cursors.Add(cursor);
            return cursor;
        }

        public IEnumerator<ResultSet.Cursor> GetEnumerator()
        {
            return (IEnumerator<ResultSet.Cursor>)cursors.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return cursors.GetEnumerator();
        }

        private List<Cursor> cursors = new List<Cursor>();

        public class Cursor : IEnumerable<Row>
        {
            private List<Row> Rows = new List<Row>();
            public int Count { get { return Rows.Count; } }
            public Row this[int index]
            {
                get
                {
                    if (Rows.Count <= index) { return Singleton<Row>.Instance; }
                    return Rows[index] as Row;
                }
            }

            public Row AddRow()
            {
                Row row = new Row();
                Rows.Add(row);
                return row;
            }

            public IEnumerator<Row> GetEnumerator()
            {
                return (IEnumerator<Row>)Rows.GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return Rows.GetEnumerator();
            }
        }
        public class Row : System.Dynamic.DynamicObject, IEnumerable
        {
            private ArrayList Columns = new ArrayList();
            private Dictionary<string, object> Members = new Dictionary<string, object>();
            public int Count { get { return Columns.Count; } }
            public object this[int index]
            {
                get
                {
                    if (Columns.Count <= index) { return null; }
                    return Columns[index];
                }
            }

            public void AddColumn(object value, string name)
            {
                Columns.Add(value);
                Members.Add(name, value);
            }
            public IEnumerator GetEnumerator()
            {
                return Columns.GetEnumerator();
            }

            public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
            {
                Members.TryGetValue(binder.Name, out result);
                if (result == DBNull.Value) { result = null; }
                return true;
            }

        }
    }
}
