using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyndnet.SFDC.ProfileMerge
{
    interface IComponentScaner
    {
        DifferenceStore Scan(DifferenceStore diffStore);
    }
}
