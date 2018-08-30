﻿using MapleLib.WzLib.Util;
using System.IO;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that has the value of a double
    /// </summary>
    public class WzDoubleProperty : AWzImageProperty {
        #region Fields

        internal string mName;
        internal double mVal;
        internal AWzObject mParent;
        internal WzImage mImgParent;

        #endregion

        #region Inherited Members

        public override object WzValue { get { return mVal; } set { mVal = (double)value; } }

        /// <summary>
        /// The parent of the object
        /// </summary>
        public override AWzObject Parent { get { return mParent; } internal set { mParent = value; } }

        /// <summary>
        /// The image that this property is contained in
        /// </summary>
        public override WzImage ParentImage { get { return mImgParent; } internal set { mImgParent = value; } }

        /// <summary>
        /// The WzPropertyType of the property
        /// </summary>
        public override WzPropertyType PropertyType { get { return WzPropertyType.Double; } }

        /// <summary>
        /// The name of this property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            pWriter.Write((byte)5);
            pWriter.Write(Value);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzDouble", Name, Value.ToString()));
        }

        public override void Dispose() {
            mName = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// The value of this property
        /// </summary>
        public double Value { get { return mVal; } set { mVal = value; } }

        /// <summary>
        /// Creates a blank WzDoubleProperty
        /// </summary>
        public WzDoubleProperty() {
        }

        /// <summary>
        /// Creates a WzDoubleProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzDoubleProperty(string pName) {
            mName = pName;
        }

        /// <summary>
        /// Creates a WzDoubleProperty with the specified name and value
        /// </summary>
        /// <param name="pName">The name of the property</param>
        /// <param name="pValue">The value of the property</param>
        public WzDoubleProperty(string pName, double pValue) {
            mName = pName;
            mVal = pValue;
        }

        #endregion

        #region Cast Values

        internal override float ToFloat(float pDef = 0) {
            return (float)mVal;
        }

        internal override double ToDouble(double pDef = 0) {
            return mVal;
        }

        internal override int ToInt(int pDef = 0) {
            return (int)mVal;
        }

        internal override short ToShort(short pDef = 0) {
            return (short)mVal;
        }

        #endregion
    }
}