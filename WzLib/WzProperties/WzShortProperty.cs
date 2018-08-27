using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A wz property which has a value which is a ushort
	/// </summary>
	public class WzShortProperty : AWzImageProperty {
		#region Fields

		internal string mName;
		internal short mVal;
		internal AWzObject mParent;
		internal WzImage mImgParent;

		#endregion

		#region Inherited Members

		public override object WzValue { get { return mVal; } set { mVal = (short) value; } }

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
		public override WzPropertyType PropertyType { get { return WzPropertyType.Short; } }

		/// <summary>
		/// The name of the property
		/// </summary>
		public override string Name { get { return mName; } set { mName = value; } }

		public override void WriteValue(WzBinaryWriter pWriter) {
			pWriter.Write((byte) 2);
			pWriter.Write(Value);
		}

		public override void ExportXml(StreamWriter pWriter, int pLevel) {
			pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzShort", Name, Value.ToString()));
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			mName = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// The value of the property
		/// </summary>
		public short Value { get { return mVal; } set { mVal = value; } }

		/// <summary>
		/// Creates a blank WzShortProperty
		/// </summary>
		public WzShortProperty() {
		}

		/// <summary>
		/// Creates a WzShortProperty with the specified name
		/// </summary>
		/// <param name="pName">The name of the property</param>
		public WzShortProperty(string pName) {
			mName = pName;
		}

		/// <summary>
		/// Creates a WzShortProperty with the specified name and value
		/// </summary>
		/// <param name="pName">The name of the property</param>
		/// <param name="pValue">The value of the property</param>
		public WzShortProperty(string pName, short pValue) {
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
			return mVal;
		}

		internal override short ToShort(short pDef = (short) 0) {
			return mVal;
		}

		#endregion
	}
}