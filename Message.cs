using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Caspar
{
    public delegate void AsyncCallback<T>(T msg);
    public delegate void AsyncCallback();
    public interface INotifier
    {
        void Response<T>(T msg);
        void Notify<T>(T msg);
        //object Args { get; set; }
    }

    

}
