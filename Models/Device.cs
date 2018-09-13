﻿using System;

namespace Devin.Models
{
    public class Device
    {
        public string Inventory { get; set; }

        public string DeviceClass { get; set; }

        public string DeviceNumber { get; set; }

        public string Name { get; set; }

        public string ComputerId { get; set; }

        public string Description { get; set; }

        public string Description1C { get; set; }

        public DateTime DateInstall { get; set; }

        public string Mol { get; set; }

        public string SerialNumber { get; set; }

        public string PassportNumber { get; set; }

        public string Location { get; set; }

        public string OS { get; set; }

        public string OSKey { get; set; }

        public int PrinterId { get; set; }

        public int Checked1C { get; set; }

        public int CheckedAida { get; set; }

        public int UUID { get; set; }

        public int GroupId { get; set; }

        public int InUse { get; set; }

        public int Deleted { get; set; }

        public string ServiceTag { get; set; }

        public string Gold { get; set; }

        public string Silver { get; set; }

        public string Platinum { get; set; }

        public string Palladium { get; set; } = "";

        public string MPG { get; set; }
    }
}