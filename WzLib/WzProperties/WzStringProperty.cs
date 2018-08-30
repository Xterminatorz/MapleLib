﻿using MapleLib.WzLib.Util;
using System.IO;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property with a string as a value
    /// </summary>
    public class WzStringProperty : AWzImageProperty {
        #region Fields

        internal string mName, mVal;
        internal AWzObject mParent;
        internal WzImage mImgParent;

        #endregion

        #region Inherited Members

        public override object WzValue { get { return mVal; } set { mVal = (string)value; } }

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
        public override WzPropertyType PropertyType { get { return WzPropertyType.String; } }

        /// <summary>
        /// The name of the property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            pWriter.Write((byte)8);
            pWriter.WriteStringValue(Value, 0, 1);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzString", Name, Value));
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public override void Dispose() {
            mName = null;
            mVal = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// The value of the property
        /// </summary>
        public string Value { get { return mVal; } set { mVal = value; } }

        /// <summary>
        /// Creates a blank WzStringProperty
        /// </summary>
        public WzStringProperty() {
        }

        /// <summary>
        /// Creates a WzStringProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzStringProperty(string pName) {
            mName = pName;
        }

        /// <summary>
        /// Creates a WzStringProperty with the specified name and value
        /// </summary>
        /// <param name="pName">The name of the property</param>
        /// <param name="pValue">The value of the property</param>
        public WzStringProperty(string pName, string pValue) {
            mName = pName;
            mVal = pValue;
        }

        #endregion

        #region Cast Values

        internal override float ToFloat(float pDef = 0) {
            return float.Parse(mVal);
        }

        internal override double ToDouble(double pDef = 0) {
            return double.Parse(mVal);
        }

        internal override int ToInt(int pDef = 0) {
            return int.Parse(mVal);
        }

        internal override short ToShort(short pDef = 0) {
            return short.Parse(mVal);
        }

        public override string ToString() {
            return mVal;
        }

        #endregion
    }
}