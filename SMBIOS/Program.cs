using System;

namespace SMBIOS
{
    class Program
    {
        static void Main(string[] args)
        {
            SMBIOSdata smbios = new SMBIOSdata();

            //smbios.GetRawData("hyper-v-2");
            smbios.GetRawData();
            //SMBIOStable tb = smbios.GetNextTable(null, true, 0, 0);
            smbios.GetTables();
            smbios.ParseTable(smbios.p_oSMBIOStables[60]);
            /*foreach(SMBIOStable table in smbios.p_oSMBIOStables)
            {
                smbios.ParseTable(table);
            }*/
        }
    }
}
