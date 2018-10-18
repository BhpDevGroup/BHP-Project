using System;
using System.Collections.Generic;
using System.Text;

namespace Bhp.Ledger
{
    [Flags]
    public enum StorageFlags : byte
    {
        None = 0,
        Constant = 0x01
    }
}
