using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBIOS
{
    public class SMBIOStable
    {
        public byte m_bTableType;
        public byte m_bFormattedSectionLength;
        public short m_wHandle;
        public byte[] p_bFormattedSection;
        public byte[] p_bUnformattedSection;
        public string[] p_sStrings;

        public SMBIOStable()
        {
            p_bFormattedSection = new byte[] { };
            p_bUnformattedSection = new byte[] { };
            p_sStrings = new string[] { };
        }
    }
}
