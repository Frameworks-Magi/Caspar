using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caspar.Network
{
    public interface IRpc
    {
        void Health();
        void OnHealth();
    }
}
