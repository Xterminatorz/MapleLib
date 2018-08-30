﻿using MapleLib.WzLib.Util;
using System.IO;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that is stored in the wz file with a byte and possibly followed by a float. If the 
    /// byte is 0, the value is 0, else the value is the float that follows.
    /// </summary>
    public class WzByteFloatProperty : AWzImageProperty {
        #region Fields

        internal string mName;
        internal float mVal;
        internal AWzObject mParent;
        internal WzImage mImgParent;

        #endregion

        #region Inherited Members

        public override object WzValue { get { return mVal; } set { mVal = (float)value; } }

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
        public override WzPropertyType PropertyType { get { return WzPropertyType.ByteFloat; } }

        /// <summary>
        /// The name of the property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            pWriter.Write((byte)4);
            if (Value == 0f) {
                pWriter.Write((byte)0);
            } else {
                pWriter.Write((byte)0x80);
                pWriter.Write(Value);
            }
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzByteFloat", Name, Value.ToString()));
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public override void Dispose() {
            mName = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// The value of the property
        /// </summary>
        public float Value { get { return mVal; } set { mVal = Value; } }

        /// <summary>
        /// Creates a blank WzByteFloatProperty
        /// </summary>
        public WzByteFloatProperty() {
        }

        /// <summary>
        /// Creates a WzByteFloatProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzByteFloatProperty(string pName) {
            mName = pName;
        }

        /// <summary>
        /// Creates a WzByteFloatProperty with the specified name and value
        /// </summary>
        /// <param name="pName">The name of the property</param>
        /// <param name="pValue">The value of the property</param>
        public WzByteFloatProperty(string pName, float pValue) {
            mName = pName;
            mVal = pValue;
        }

        #endregion

        #region Cast Values

        internal override float ToFloat(float pDef = 0) {
            return mVal;
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