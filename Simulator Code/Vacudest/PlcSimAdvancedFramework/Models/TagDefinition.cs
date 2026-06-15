using System;
using System.Globalization;
using System.Xml.Serialization;
using PlcSimAdvancedFramework.ViewModels;

namespace PlcSimAdvancedFramework.Models
{
    public class TagDefinition : ViewModelBase
    {
        private string displayName;
        private string address;
        private string manualValue;
        private string lastReadValue;
        private bool manualEnabled;
        private TagDataType dataType;
        private string description;

        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        public string Address
        {
            get { return address; }
            set { SetProperty(ref address, value); }
        }

        public TagDataType DataType
        {
            get { return dataType; }
            set { SetProperty(ref dataType, value); }
        }

        public string ManualValue
        {
            get { return manualValue; }
            set { SetProperty(ref manualValue, value); }
        }

        [XmlIgnore]
        public string LastReadValue
        {
            get { return lastReadValue; }
            set { SetProperty(ref lastReadValue, value); }
        }

        public bool ManualEnabled
        {
            get { return manualEnabled; }
            set { SetProperty(ref manualEnabled, value); }
        }

        public string Description
        {
            get { return description; }
            set { SetProperty(ref description, value); }
        }

        public object ParseManualValue()
        {
            switch (DataType)
            {
                case TagDataType.Bool:
                    return bool.Parse(ManualValue ?? "false");
                case TagDataType.Int16:
                    return short.Parse(ManualValue ?? "0", CultureInfo.InvariantCulture);
                case TagDataType.Float:
                    return float.Parse(ManualValue ?? "0", CultureInfo.InvariantCulture);
                default:
                    throw new InvalidOperationException("Unsupported data type.");
            }
        }
    }
}
