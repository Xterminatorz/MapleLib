using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MapleLib.WzLib.Util;
using System;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that's value is a string
	/// </summary>
	public class WzUOLProperty : AWzImageProperty, IExtended {
		#region Fields

		internal string mName, mVal;
		internal AWzObject mParent;
		internal WzImage mImgParent;
		internal AWzImageProperty mLinkVal;

		#endregion

		#region Inherited Members

		public override object WzValue { get { return mVal; } set { mVal = (string) value; } }

		/// <summary>
		/// The parent of the object
		/// </summary>
		public override AWzObject Parent { get { return mParent; } internal set { mParent = value; } }

		/// <summary>
		/// The image that this property is contained in
		/// </summary>
		public override WzImage ParentImage { get { return mImgParent; } internal set { mImgParent = value; } }

		/// <summary>
		/// The name of the property
		/// </summary>
		public override string Name { get { return mName; } set { mName = value; } }

        public override List<AWzImageProperty> WzProperties { get { return LinkValue != null ? LinkValue.WzProperties : null; } }

        public override AWzImageProperty this[string pName] { get { return LinkValue[pName]; } }

		public override AWzImageProperty GetFromPath(string pPath) {
			return LinkValue.GetFromPath(pPath);
		}

		/// <summary>
		/// The WzPropertyType of the property
		/// </summary>
		public override WzPropertyType PropertyType { get { return WzPropertyType.UOL; } }

		public override void WriteValue(WzBinaryWriter pWriter) {
			pWriter.WriteStringValue("UOL", 0x73, 0x1B);
			pWriter.Write((byte) 0);
			pWriter.WriteStringValue(Value, 0, 1);
		}

		public override void ExportXml(StreamWriter pWriter, int pLevel) {
			pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedValuePair("WzUOL", Name, Value));
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			mName = null;
            mVal = null;
            mLinkVal = null;
        }

		#endregion

		#region Custom Members

		/// <summary>
		/// The value of the property
		/// </summary>
		public string Value { get { return mVal; } set { mVal = value; } }

		public AWzImageProperty LinkValue {
			get {
				if (mLinkVal == null) {
					AWzObject curObj = Parent;
					string[] seperatedPath = mVal.Split('/');
					foreach (string t in seperatedPath) {
						if (curObj == null)
							return null;
                        string trimmedName = t.Trim();
						if (trimmedName.Equals("..")) {
							curObj = curObj.Parent;
							continue;
						}
						switch (curObj.ObjectType) {
                            case WzObjectType.File:
							case WzObjectType.Directory:
								curObj = ((WzDirectory) curObj)[trimmedName];
								continue;
							case WzObjectType.Image:
								curObj = ((WzImage) curObj)[trimmedName];
								continue;
							case WzObjectType.Property:
								switch (((AWzImageProperty) curObj).PropertyType) {
									case WzPropertyType.Canvas:
										curObj = ((WzCanvasProperty) curObj)[trimmedName];
										continue;
									case WzPropertyType.Convex:
										curObj = ((WzConvexProperty) curObj)[trimmedName];
										continue;
									case WzPropertyType.SubProperty:
										curObj = ((WzSubProperty) curObj)[trimmedName];
										continue;
									case WzPropertyType.Vector:
										if (trimmedName == "X")
											return ((WzVectorProperty) curObj).X;
										return trimmedName == "Y" ? ((WzVectorProperty) curObj).Y : null;
									default:
										return null;
								}
							default:
								return null;
						}
					}
                    mLinkVal = (AWzImageProperty)curObj;
                    if (mLinkVal != null)
                        mImgParent.AddReferencedImage(mLinkVal.ParentImage);
                }
				return mLinkVal;
			}
		}

		/// <summary>
		/// Creates a blank WzUOLProperty
		/// </summary>
		public WzUOLProperty() {
		}

		/// <summary>
		/// Creates a WzUOLProperty with the specified name
		/// </summary>
		/// <param name="pName">The name of the property</param>
		public WzUOLProperty(string pName) {
			mName = pName;
		}

		/// <summary>
		/// Creates a WzUOLProperty with the specified name and value
		/// </summary>
		/// <param name="pName">The name of the property</param>
		/// <param name="pValue">The value of the property</param>
		public WzUOLProperty(string pName, string pValue) {
			mName = pName;
			mVal = pValue;
		}

		#endregion

		#region Cast Values

		internal override Bitmap ToBitmap(Bitmap pDef = null) {
			return LinkValue.ToBitmap(pDef);
		}


		internal override byte[] ToBytes(byte[] pDef = null) {
			return LinkValue.ToBytes(pDef);
		}

		internal override double ToDouble(double pDef = 0) {
			return LinkValue.ToDouble(pDef);
		}

		internal override float ToFloat(float pDef = 0) {
			return LinkValue.ToFloat(pDef);
		}

		internal override int ToInt(int pDef = 0) {
			return LinkValue.ToInt(pDef);
		}

		internal override WzPngProperty ToPngProperty(WzPngProperty pDef = null) {
			return LinkValue.ToPngProperty(pDef);
		}

		internal override Point ToPoint(int pXDef = 0, int pYDef = 0) {
			return LinkValue.ToPoint(pXDef, pYDef);
		}

		public override string ToString() {
			return LinkValue.ToString();
		}

		internal override short ToShort(short pDef = (short) 0) {
			return LinkValue.ToShort(pDef);
		}

		#endregion
	}
}