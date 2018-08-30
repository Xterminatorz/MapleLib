using MapleLib.WzLib.Util;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that can contain sub properties and has one png image
    /// </summary>
    public class WzCanvasProperty : APropertyContainer {
        #region Fields

        internal List<AWzImageProperty> mProperties = new List<AWzImageProperty>();
        internal WzPngProperty mImageProp;
        internal string mName;
        internal AWzObject mParent;
        internal WzImage mImgParent;
        internal string _inlink;
        internal WzCanvasProperty _inlinkValue;
        internal string _outlink;
        internal WzCanvasProperty _outlinkValue;

        #endregion

        #region Inherited Members

        public override object WzValue { get { return PngProperty; } set { mImageProp.WzValue = value; } }

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
        public override WzPropertyType PropertyType { get { return WzPropertyType.Canvas; } }

        /// <summary>
        /// The properties contained in this property
        /// </summary>
        public override List<AWzImageProperty> WzProperties { get { return mProperties; } }

        /// <summary>
        /// The inlink contained in this property
        /// </summary>
        public string Inlink { get { return _inlink; } }

        public WzCanvasProperty InlinkValue {
            get {
                if (_inlink == null) return null;
                if (_inlinkValue == null) {
                    AWzObject curObj = mImgParent;
                    string[] seperatedPath = _inlink.Split('/');
                    foreach (string t in seperatedPath) {
                        if (curObj == null)
                            return null;
                        string trimmedName = t.Trim();
                        switch (curObj.ObjectType) {
                            case WzObjectType.Image:
                                curObj = ((WzImage)curObj)[trimmedName];
                                continue;
                            case WzObjectType.Property:
                                switch (((AWzImageProperty)curObj).PropertyType) {
                                    case WzPropertyType.Canvas:
                                        curObj = ((WzCanvasProperty)curObj)[trimmedName];
                                        continue;
                                    case WzPropertyType.SubProperty:
                                        curObj = ((WzSubProperty)curObj)[trimmedName];
                                        continue;
                                    default:
                                        return null;
                                }
                            default:
                                return null;
                        }
                    }
                    _inlinkValue = (WzCanvasProperty)curObj;
                }
                return _inlinkValue;
            }
        }

        /// <summary>
        /// The outlink contained in this property
        /// </summary>
        public string Outlink { get { return _outlink; } }

        public WzCanvasProperty OutlinkValue {
            get {
                if (_outlink == null) return null;
                if (_outlinkValue == null || _outlinkValue != null && _outlinkValue.Name == null) { // Relocate if referenced value was disposed
                    AWzObject curObj = mImgParent;
                    while (curObj.Parent != null)
                        curObj = curObj.Parent;
                    string[] seperatedPath = _outlink.Substring(_outlink.IndexOf("/") + 1).Split('/');
                    foreach (string t in seperatedPath) {
                        if (curObj == null)
                            return null;
                        string trimmedName = t.Trim();
                        switch (curObj.ObjectType) {
                            case WzObjectType.File:
                            case WzObjectType.Directory:
                                curObj = ((WzDirectory)curObj)[trimmedName];
                                continue;
                            case WzObjectType.Image:
                                curObj = ((WzImage)curObj)[trimmedName];
                                continue;
                            case WzObjectType.Property:
                                switch (((AWzImageProperty)curObj).PropertyType) {
                                    case WzPropertyType.Canvas:
                                        curObj = ((WzCanvasProperty)curObj)[trimmedName];
                                        continue;
                                    case WzPropertyType.SubProperty:
                                        curObj = ((WzSubProperty)curObj)[trimmedName];
                                        continue;
                                    default:
                                        return null;
                                }
                            default:
                                return null;
                        }
                    }
                    if (curObj != null) {
                        _outlinkValue = (WzCanvasProperty)curObj;
                        mImgParent.AddReferencedImage(_outlinkValue.ParentImage);
                    }
                }
                return _outlinkValue;
            }
        }

        /// <summary>
        /// The properties contained in this property
        /// </summary>
        public override void AddProperties(List<AWzImageProperty> pProps) {
            foreach (AWzImageProperty prop in pProps) {
                AddProperty(prop);
                if (prop.PropertyType.Equals(WzPropertyType.String)) {
                    var stringProp = (WzStringProperty)prop;
                    if (stringProp.Name.Contains("_inlink")) {
                        _inlink = stringProp.Value;
                        TrimLinkSpaces(ref _inlink);
                        break;
                    }
                    if (stringProp.Name.Contains("_outlink")) {
                        _outlink = stringProp.Value;
                        TrimLinkSpaces(ref _outlink);
                        break;
                    }
                }
            }
        }

        private static void TrimLinkSpaces(ref string link) {
            string sanitizedLink = string.Empty;
            string[] seperatedPath = link.Split('/');
            foreach (string t in seperatedPath) {
                sanitizedLink += t.Trim() + "/";
            }
            link = sanitizedLink.Substring(0, sanitizedLink.Length - 1);
        }

        /// <summary>
        /// The name of the property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        /// <summary>
        /// Gets a wz property by it's name
        /// </summary>
        /// <param name="pWriter">The name of the property</param>
        /// <returns>The wz property with the specified name</returns>
        public override void WriteValue(WzBinaryWriter pWriter) {
            pWriter.WriteStringValue("Canvas", 0x73, 0x1B);
            pWriter.Write((byte)0);
            if (mProperties.Count > 0) {
                pWriter.Write((byte)1);
                WritePropertyList(pWriter, mProperties);
            } else {
                pWriter.Write((byte)0);
            }
            pWriter.WriteCompressedInt(PngProperty.Width);
            pWriter.WriteCompressedInt(PngProperty.Height);
            pWriter.WriteCompressedInt(PngProperty.mFormat);
            pWriter.Write((byte)PngProperty.mFormat2);
            pWriter.Write(0);
            byte[] bytes = PngProperty.GetCompressedBytes();
            pWriter.Write(bytes.Length + 1);
            pWriter.Write((byte)0);
            pWriter.Write(bytes);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.OpenNamedTag("WzCanvas", Name, false, false) + XmlUtil.Attrib("width", PngProperty.Width.ToString()) + XmlUtil.Attrib("height", PngProperty.Height.ToString(), true, false));
            DumpPropertyList(pWriter, pLevel, WzProperties);
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.CloseTag("WzCanvas"));
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public override void Dispose() {
            mName = null;
            _inlink = null;
            _inlinkValue = null;
            _outlink = null;
            if (_outlinkValue != null)
                mImgParent.AddReferencedImage(_outlinkValue.ParentImage);
            _outlinkValue = null;
            mImageProp.Dispose();
            mImageProp = null;
            foreach (AWzImageProperty prop in mProperties) {
                prop.Dispose();
            }
            mProperties.Clear();
            mProperties = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// The png image for this canvas property
        /// </summary>
        public WzPngProperty PngProperty { get { return mImageProp; } set { mImageProp = value; } }

        /// <summary>
        /// Creates a blank WzCanvasProperty
        /// </summary>
        public WzCanvasProperty() {
        }

        /// <summary>
        /// Creates a WzCanvasProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzCanvasProperty(string pName) {
            mName = pName;
        }

        #endregion

        #region Cast Values

        internal override WzPngProperty ToPngProperty(WzPngProperty pDef = null) {
            return mImageProp;
        }

        internal override Bitmap ToBitmap(Bitmap pDef = null) {
            return mImageProp.GetPNG();
        }

        #endregion
    }
}