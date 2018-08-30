using MapleLib.WzLib.Util;
using System.IO;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that is stored in the wz file with a signed byte and possibly followed by an int. If the 
    /// signed byte is equal to -128, the value is is the long that follows, else the value is the byte.
    /// </summary>
    public class WzCompressedLongProperty : AWzImageProperty {
        #region Fields

        internal string mName;
        internal long mVal;
        internal AWzObject mParent;
        internal WzImage mImgParent;

        #endregion

        #region Inherited Members

        public override object WzValue { get { return mVal; } set { mVal = (long)value; } }

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
        public override WzPropertyType PropertyType { get { return WzPropertyType.CompressedLong; } }

        /// <summary>
        /// The name of the property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            pWriter.Write((byte)20);
            pWriter.WriteCompressedLong(Value);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzCompressedLong", Name, Value.ToString()));
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
        public long Value { get { return mVal; } set { mVal = value; } }

        /// <summary>
        /// Creates a blank WzCompressedLongProperty
        /// </summary>
        public WzCompressedLongProperty() {
        }

        /// <summary>
        /// Creates a WzCompressedLongProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzCompressedLongProperty(string pName) {
            mName = pName;
        }

        /// <summary>
        /// Creates a WzCompressedLongProperty with the specified name and value
        /// </summary>
        /// <param name="pName">The name of the property</param>
        /// <param name="pValue">The value of the property</param>
        public WzCompressedLongProperty(string pName, long pValue) {
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