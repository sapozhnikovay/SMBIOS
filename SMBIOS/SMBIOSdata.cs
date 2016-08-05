using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace SMBIOS
{
    public class SMBIOSdata
    {
        private byte m_byMajorVersion;
        private byte m_byMinorVersion;
        private int m_dwLen;
        private byte[] m_pbBIOSData;
        public List<SMBIOStable> p_oSMBIOStables;
        private const string OUT_OF_SPEC = "<OUT OF SPEC>";

        public SMBIOSdata() {
            m_pbBIOSData = new byte[] { };
            p_oSMBIOStables = new List<SMBIOStable>();
        }

        public void GetRawData(string hostname = "localhost")
        {
            try
            {
                ManagementScope scope = new ManagementScope("\\\\" + hostname + "\\root\\WMI");
                scope.Connect();
                ObjectQuery wmiquery = new ObjectQuery("SELECT * FROM MSSmBios_RawSMBiosTables");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, wmiquery);
                ManagementObjectCollection coll = searcher.Get();
                foreach (ManagementObject queryObj in coll)
                {
                    if (queryObj["SMBiosData"] != null) m_pbBIOSData = (byte[])(queryObj["SMBiosData"]);
                    if (queryObj["SmbiosMajorVersion"] != null) m_byMajorVersion = (byte)(queryObj["SmbiosMajorVersion"]);
                    if (queryObj["SmbiosMinorVersion"] != null) m_byMinorVersion = (byte)(queryObj["SmbiosMinorVersion"]);
                    //if (queryObj["Size"] != null) m_dwLen = (long)(queryObj["Size"]);
                    m_dwLen = m_pbBIOSData.Length;
                }
            }
            catch
            {
            }
        }

                
        public void GetTables()
        {
            int i = 0;
            while(i < m_dwLen)
            {
                SMBIOStable p_oTable = new SMBIOStable();
                p_oTable.m_bTableType = m_pbBIOSData[i];
                p_oTable.m_bFormattedSectionLength = m_pbBIOSData[i + 1];
                p_oTable.m_wHandle = BitConverter.ToInt16(m_pbBIOSData, i + 2);

                int wUnformattedSectionStart = i + p_oTable.m_bFormattedSectionLength;
                p_oTable.p_bFormattedSection = m_pbBIOSData.Skip(i).Take(p_oTable.m_bFormattedSectionLength).ToArray();
                
                for(int j = i + p_oTable.m_bFormattedSectionLength; ;j++)
                {
                    if((m_pbBIOSData[j] == 0) && (m_pbBIOSData[j+1] == 0))
                    {
                        p_oTable.p_bUnformattedSection = m_pbBIOSData.Skip(i+p_oTable.m_bFormattedSectionLength).Take(j-i-p_oTable.m_bFormattedSectionLength).ToArray();
                        i = j + 2;
                        break;
                    }
                }
                if (p_oTable.p_bUnformattedSection.Length > 0) p_oTable.p_sStrings = Encoding.ASCII.GetString(p_oTable.p_bUnformattedSection).Split('\0');
                p_oSMBIOStables.Add(p_oTable);
            }
            Console.WriteLine("SMBIOS " + m_byMajorVersion + "." + m_byMinorVersion + " present.");
            Console.WriteLine(p_oSMBIOStables.Count + " structures occupying " + m_dwLen + " bytes.");
        }

        public void ParseTable(SMBIOStable table)
        {
            Console.WriteLine("\nHandle " + table.m_wHandle + ", DMI type " + table.m_bTableType + ", " + table.m_bFormattedSectionLength + " bytes");
            switch (table.m_bTableType)
            {
                case 0: //BIOS
                    Console.WriteLine("BIOS information");
                    Console.WriteLine("\tVendor: " + table.p_sStrings[table.p_bFormattedSection[4] - 1]);
                    Console.WriteLine("\tBIOS Version: " + table.p_sStrings[table.p_bFormattedSection[5] - 1]);
                    Console.WriteLine("\tBIOS Release date: " + table.p_sStrings[table.p_bFormattedSection[8] - 1]);
                    break;
                case 1: //System information
                    Console.WriteLine("System information");
                    Console.WriteLine("\tManufacturer: " + table.p_sStrings[table.p_bFormattedSection[4] - 1]);
                    Console.WriteLine("\tProduct Name: " + table.p_sStrings[table.p_bFormattedSection[5] - 1]);
                    Console.WriteLine("\tVersion: " + (table.p_bFormattedSection[6] != 0 ? table.p_sStrings[table.p_bFormattedSection[6] - 1] : ""));
                    Console.WriteLine("\tSerial Number: " + table.p_sStrings[table.p_bFormattedSection[7] - 1]);
                    Console.WriteLine("\tUUID: " + dmi_system_uuid(new ArraySegment<byte>(table.p_bFormattedSection,8,16).ToArray(), (ushort)(m_byMajorVersion + (m_byMinorVersion << 8))));
                    Console.WriteLine("\tWake-up type: " + dmi_system_wake_up_type(table.p_bFormattedSection[24]));
                    Console.WriteLine("\tSKU Number: " + (table.p_bFormattedSection[25] != 0 ? table.p_sStrings[table.p_bFormattedSection[25] - 1] : ""));
                    Console.WriteLine("\tFamily: " + (table.p_bFormattedSection[26] != 0 ? table.p_sStrings[table.p_bFormattedSection[26] - 1] : ""));
                    break;
                case 4: //Processor
                    Console.WriteLine("Procesor information");
                    Console.WriteLine("\tSocket Designation: " + table.p_sStrings[table.p_bFormattedSection[4]-1]);
                    Console.WriteLine("\tType: " + dmi_processor_type(table.p_bFormattedSection[5]));
                    Console.WriteLine("\tFamily: " + dmi_processor_family(table.p_bFormattedSection[6]));
                    Console.WriteLine("\tVoltage: " + dmi_processor_voltage(table.p_bFormattedSection[17]));
                    Console.WriteLine("\tUpgrade: " + dmi_processor_upgrade(table.p_bFormattedSection[25]));
                    break;
                case 9: //System slot
                    Console.WriteLine("System slot information");
                    Console.WriteLine("\tSlot designation: " + table.p_sStrings[table.p_bFormattedSection[4]-1]);
                    Console.WriteLine("\tSlot type: " +dmi_slot_type(table.p_bFormattedSection[5]));
                    Console.WriteLine("\tSlot Data Bus Width: " + dmi_slot_bus_width(table.p_bFormattedSection[6]));
                    Console.WriteLine("\tCurrent usage: " + dmi_slot_usage(table.p_bFormattedSection[7]));
                    Console.WriteLine("\tSlot length: " + dmi_slot_length(table.p_bFormattedSection[8]));
                    Console.WriteLine("\tSlot ID: " + dmi_slot_id(table.p_bFormattedSection[9], table.p_bFormattedSection[10], table.p_bFormattedSection[5]));
                    Console.WriteLine("\tSlot Characteristics: " + dmi_slot_characteristics(table.p_bFormattedSection[11], table.p_bFormattedSection[12]));
                    //Console.WriteLine("\tBus Address: " + table.p_bFormattedSection[13] + ":" + table.p_bFormattedSection[15] + ":" + table.p_bFormattedSection[16]);
                    if (table.m_bFormattedSectionLength < 0x11) break;
                    Console.WriteLine("\tBus Address: " + dmi_slot_segment_bus_func((ushort)(table.p_bFormattedSection[13] + (table.p_bFormattedSection[14] << 8)), table.p_bFormattedSection[15], table.p_bFormattedSection[16]));
                    break;
                case 10: //On Board Devices Information
                    Console.WriteLine("Onboard device information");
                    break;
                case 12: //System Configuration Options (Type 12)
                    Console.WriteLine("System Configuration Options");
                    Console.WriteLine(dmi_system_configuration_options(table.p_bFormattedSection[4], table.p_sStrings));
                    break;
                case 13: //BIOS Language information
                    Console.WriteLine("BIOS Language information");
                    Console.WriteLine("\tAvailable Languages: " + dmi_bios_languages(table.p_bFormattedSection[4], table.p_sStrings));
                    Console.WriteLine("\tLanguage format: " + dmi_bios_language_format(table.p_bFormattedSection[5]));
                    Console.WriteLine("\tCurrent Language: " + table.p_sStrings[table.p_bFormattedSection[21] - 1]);
                    break;
                case 21: //Built-in Pointing Device (Type 21)
                    Console.WriteLine("Built-in Pointing Device");
                    Console.WriteLine("\tType: " + dmi_pointing_device_type(table.p_bFormattedSection[4]));
                    Console.WriteLine("\tInterface: " + dmi_pointing_device_interface(table.p_bFormattedSection[5]));
                    Console.WriteLine("\tButtons: " + table.p_bFormattedSection[6]);
                    break;
                default:
                    Console.WriteLine("Unsupported table type.");
                    break;
            }
        }

        private string dmi_processor_type(byte code)
        {
            string[] type = {   "Other", /* 0x01 */
		                        "Unknown",
                                "Central Processor",
                                "Math Processor",
                                "DSP Processor",
                                "Video Processor" /* 0x06 */
                            };
            if (code >= 0x01 && code <= 0x06)
                return type[code - 0x01];
            return OUT_OF_SPEC;
        }

        private string dmi_processor_family(byte code)
        {
            return "";
        }

        private string dmi_processor_voltage(byte code)
        {
            /* 7.5.4 */
            string[] voltage = {
                "5.0 V", /* 0 */
		        "3.3 V",
                "2.9 V" /* 2 */
	        };
            int i;
            string result = "";

            if ((code & (1 << 7)) != 0)
                result += (float)(code & 0x7f) / 10 + " V";
            else
            {
                for (i = 0; i <= 2; i++)
                    if ((code & (1 << i)) != 0)
                        result += voltage[i];
                if (code == 0x00)
                    result = " Unknown";
            }
            return result;
        }

        private string dmi_processor_upgrade(byte code)
        {
            string[] upgrade = {
                "Other", /* 0x01 */
		        "Unknown",
                "Daughter Board",
                "ZIF Socket",
                "Replaceable Piggy Back",
                "None",
                "LIF Socket",
                "Slot 1",
                "Slot 2",
                "370-pin Socket",
                "Slot A",
                "Slot M",
                "Socket 423",
                "Socket A (Socket 462)",
                "Socket 478",
                "Socket 754",
                "Socket 940",
                "Socket 939",
                "Socket mPGA604",
                "Socket LGA771",
                "Socket LGA775",
                "Socket S1",
                "Socket AM2",
                "Socket F (1207)",
                "Socket LGA1366",
                "Socket G34",
                "Socket AM3",
                "Socket C32",
                "Socket LGA1156",
                "Socket LGA1567",
                "Socket PGA988A",
                "Socket BGA1288",
                "Socket rPGA988B",
                "Socket BGA1023",
                "Socket BGA1224",
                "Socket BGA1155",
                "Socket LGA1356",
                "Socket LGA2011",
                "Socket FS1",
                "Socket FS2",
                "Socket FM1",
                "Socket FM2",
                "Socket LGA2011-3",
                "Socket LGA1356-3", /* 0x2C */
                "Socket LGA1150",
                "Socket BGA1168",
                "Socket BGA1234",
                "Socket BGA1364" /* 0x30 */
            };

            if (code >= 0x01 && code <= 0x30)
                return upgrade[code - 0x01];
            return OUT_OF_SPEC;
        }

        private string dmi_slot_type(byte code)
        {
            string[] type =
            {
                "Other", /* 0x01 */
		        "Unknown",
                "ISA",
                "MCA",
                "EISA",
                "PCI",
                "PC Card (PCMCIA)",
                "VLB",
                "Proprietary",
                "Processor Card",
                "Proprietary Memory Card",
                "I/O Riser Card",
                "NuBus",
                "PCI-66",
                "AGP",
                "AGP 2x",
                "AGP 4x",
                "PCI-X",
                "AGP 8x" /* 0x13 */
            };

            string[] type_0xA0 =
            {
                "PC-98/C20", /* 0xA0 */
		        "PC-98/C24",
                "PC-98/E",
                "PC-98/Local Bus",
                "PC-98/Card",
                "PCI Express",
                "PCI Express x1",
                "PCI Express x2",
                "PCI Express x4",
                "PCI Express x8",
                "PCI Express x16",
                "PCI Express 2",
                "PCI Express 2 x1",
                "PCI Express 2 x2",
                "PCI Express 2 x4",
                "PCI Express 2 x8",
                "PCI Express 2 x16",
                "PCI Express 3",
                "PCI Express 3 x1",
                "PCI Express 3 x2",
                "PCI Express 3 x4",
                "PCI Express 3 x8",
                "PCI Express 3 x16" /* 0xB6 */
            };

            if (code >= 0x01 && code <= 0x13)
                return type[code - 0x01];
            if (code >= 0xA0 && code <= 0xB6)
                return type_0xA0[code - 0xA0];
            return OUT_OF_SPEC;
        }

        private string dmi_slot_bus_width(byte code)
        {
            string[] width =
            {
                "Other", /* 0x01, "Other" */
		        "Unknown", /* "Unknown" */
		        "8-bit ",
                "16-bit ",
                "32-bit ",
                "64-bit ",
                "128-bit ",
                "x1 ",
                "x2 ",
                "x4 ",
                "x8 ",
                "x12 ",
                "x16 ",
                "x32 " /* 0x0E */
            };
            return width[code - 1];
        }

        private string dmi_slot_length(byte code)
        {
            string[] length =
            {
                "Other", /* 0x01, "Other" */
		        "Unknown", /* "Unknown" */
		        "Short Length ",
                "Long Length ",
                "2.5\" drive form factor ",
                "3.5\" drive form factor "
            };
            return length[code - 1];
        }

        private string dmi_slot_usage(byte code)
        {
            string[] usage =
            {
                "Other", /* 0x01, "Other" */
		        "Unknown", /* "Unknown" */
		        "Available ",
                "In use "
            };
            return usage[code - 1];
        }

        private string dmi_slot_characteristics(byte code1, byte code2)
        {
            string result = "";
            //First byte
            if ((code1 & (1 << 0)) != 0) { result += "\n\t\tCharacteristics unknown"; }
            if ((code1 & (1 << 1)) != 0) { result += "\n\t\tProvides 5.0 volts."; }
            if ((code1 & (1 << 2)) != 0) { result += "\n\t\tProvides 3.3 volts."; }
            if ((code1 & (1 << 3)) != 0) { result += "\n\t\tSlot’s opening is shared with another slot (for example, PCI/EISA shared slot)."; }
            if ((code1 & (1 << 4)) != 0) { result += "\n\t\tPC Card slot supports PC Card-16."; }
            if ((code1 & (1 << 5)) != 0) { result += "\n\t\tPC Card slot supports CardBus."; }
            if ((code1 & (1 << 6)) != 0) { result += "\n\t\tPC Card slot supports Zoom Video."; }
            if ((code1 & (1 << 7)) != 0) { result += "\n\t\tPC Card slot supports Modem Ring Resume."; }
            //Second byte
            if ((code2 & (1 << 0)) != 0) { result += "\n\t\tPCI slot supports Power Management Event (PME#) signal."; }
            if ((code2 & (1 << 1)) != 0) { result += "\n\t\tSlot supports hot-plug devices."; }
            if ((code2 & (1 << 2)) != 0) { result += "\n\t\tPCI slot supports SMBus signal."; }

            return result;
        }

        private string dmi_slot_id(byte code1, byte code2, byte type)
        {
            /* 7.10.5 */
            string result = "";
            switch (type)
            {
                case 0x04: /* MCA */
                    result += "\tID: " + code1 + "\n";
                    break;
                case 0x05: /* EISA */
                    result += "\tID: " + code1 + "\n";
                    break;
                case 0x06: /* PCI */
                case 0x0E: /* PCI */
                case 0x0F: /* AGP */
                case 0x10: /* AGP */
                case 0x11: /* AGP */
                case 0x12: /* PCI-X */
                case 0x13: /* AGP */
                case 0xA5: /* PCI Express */
                case 0xA6: /* PCI Express */
                case 0xA7: /* PCI Express */
                case 0xA8: /* PCI Express */
                case 0xA9: /* PCI Express */
                case 0xAA: /* PCI Express */
                case 0xAB: /* PCI Express 2 */
                case 0xAC: /* PCI Express 2 */
                case 0xAD: /* PCI Express 2 */
                case 0xAE: /* PCI Express 2 */
                case 0xAF: /* PCI Express 2 */
                case 0xB0: /* PCI Express 2 */
                case 0xB1: /* PCI Express 3 */
                case 0xB2: /* PCI Express 3 */
                case 0xB3: /* PCI Express 3 */
                case 0xB4: /* PCI Express 3 */
                case 0xB5: /* PCI Express 3 */
                case 0xB6: /* PCI Express 3 */
                    result += "\tID: " + code1 + "\n";
                    break;
                case 0x07: /* PCMCIA */
                    result += "\tID: Adapter " + code1 + ", Socket " + code2 + "\n";
                    break;
            }
            return result;
        }

        private string dmi_slot_segment_bus_func(ushort code1, byte code2, byte code3)
        {
            /* 7.10.8 */
            string result = "";
            if (!(code1 == 0xFFFF && code2 == 0xFF && code3 == 0xFF))
                result += string.Format("{0:x04}:{1:x02}:{2:x02}.{3:x}\n",
                       code1, code2, code3 >> 3, code3 & 0x7);
            return result;
        }

        private string dmi_system_wake_up_type(byte code)
        {
            string[] type =
            {
                "Reserved", /* 0x00 */
		        "Other",
                "Unknown",
                "APM Timer",
                "Modem Ring",
                "LAN Remote",
                "Power Switch",
                "PCI PME#",
                "AC Power Restored" /* 0x08 */
            };
            return type[code - 1];
        }

        private string dmi_system_uuid(byte[] p, ushort ver) {
            bool only0xFF = true, only0x00 = true;
            int i;
            string result = "";

            for (i = 0; i < 16 && (only0x00 || only0xFF); i++)
            {
                if (p[i] != 0x00) only0x00 = false;
                if (p[i] != 0xFF) only0xFF = false;
            }

            if (only0xFF)
            {
                result+="Not Present";
                return result;
            }
            if (only0x00)
            {
                result+="Not Settable";
                return result;
            }

            if (ver >= 0x0206)
                result += string.Format("{0:X02}{1:X02}{2:X02}{3:X02}-{4:X02}{5:X02}-{6:X02}{7:X02}-{8:X02}{9:X02}-{10:X02}{11:X02}{12:X02}{13:X02}{14:X02}{15:X02}",
                    p[3], p[2], p[1], p[0], p[5], p[4], p[7], p[6],
                    p[8], p[9], p[10], p[11], p[12], p[13], p[14], p[15]);
            else
                result += string.Format("{0:X02}{1:X02}{2:X02}{3:X02}-{4:X02}{5:X02}-{6:X02}{7:X02}-{8:X02}{9:X02}-{10:X02}{11:X02}{12:X02}{13:X02}{14:X02}{15:X02}",
                    p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7],
                    p[8], p[9], p[10], p[11], p[12], p[13], p[14], p[15]);

            return result;
        }

        /*
        * 7.14 BIOS Language Information (Type 13)
        */
        private string dmi_bios_language_format(byte code)
        {
            if (code == 0x01)
                return "Abbreviated";
            else
                return "Long";
        }

        private string dmi_bios_languages(byte count, string[] strings)
        {
            string result = "";

            for(var i = 0; i < count; i++)
            {
                result += "\t" + strings[i];
                if (i > 0) result += "\n";
            }

            return result;
        }

        /*
        * 7.13 System Configuration Options (Type 12)
        */
        private string dmi_system_configuration_options(byte count, string[] strings)
        {
            string result = "";

            for (var i = 0; i < count; i++)
            {
                result += "\tOption " + i + ": " + strings[i];
                if (count > 1) result += "\n";
            }

            return result;
        }

        /*
        * Built-in Pointing Device (Type 21)
        */
        private string dmi_pointing_device_type(byte code)
        {
            string[] type =
            {
                "Other", /* 0x01 */
		        "Unknown",
                "Mouse",
                "Track Ball",
                "Track Point",
                "Glide Point",
                "Touch Pad",
                "Touch Screen",
                "Optical Sensor" /* 0x09 */
            };
            if (code >= 0x01 && code <= 0x09)
                return type[code - 0x01];
            return OUT_OF_SPEC;
        }

        private string dmi_pointing_device_interface(byte code)
        {
            string[] interface0x00 =
            {
                "Other", /* 0x01 */
		        "Unknown",
                "Serial",
                "PS/2",
                "Infrared",
                "HIP-HIL",
                "Bus Mouse",
                "ADB (Apple Desktop Bus)" /* 0x08 */
            };
            string[] interface0xA0 =
            {
                "Bus Mouse DB-9", /* 0xA0 */
		        "Bus Mouse Micro DIN",
                "USB" /* 0xA2 */
            };
            if (code >= 0x01 && code <= 0x08)
                return interface0x00[code - 0x01];
	        if (code >= 0xA0 && code <= 0xA2)
		        return interface0xA0[code - 0xA0];
	        return OUT_OF_SPEC;
        }

        /*
        * 7.11 On Board Devices Information (Type 10)
        */
        private string dmi_on_board_devices_type(byte code)
        {
            string[] type =
            {
                "Other", /* 0x01 */
		        "Unknown",
                "Video",
                "SCSI Controller",
                "Ethernet",
                "Token Ring",
                "Sound",
                "PATA Controller",
                "SATA Controller",
                "SAS Controller" /* 0x0A */
            };
            if (code >= 0x01 && code <= 0x0A)
                return type[code - 0x01];
            return OUT_OF_SPEC;
        }


    }
}
