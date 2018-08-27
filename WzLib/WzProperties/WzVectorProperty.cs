﻿using System.Drawing;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that contains an x and a y value
	/// </summary>
	public class WzVectorProperty : AWzImageProperty, IExtended {
		#region Fields

		internal string name;
		internal WzCompressedIntProperty x, y;
		internal AWzObject parent;
		internal WzImage imgParent;

		#endregion

		#region Inherited Members

		public override object WzValue {
			get { return new Point(x.Value, y.Value); }
			set {
				if (value is Point) {
					x.mVal = ((Point) value).X;
					y.mVal = ((Point) value).Y;
				} else {
					x.mVal = ((Size) value).Width;
					y.mVal = ((Size) value).Height;
				}
			}
		}

		/// <summary>
		/// The parent of the object
		/// </summary>
		public override AWzObject Parent { get { return parent; } internal set { parent = value; } }

		/// <summary>
		/// The image that this property is contained in
		/// </summary>
		public override WzImage ParentImage { get { return imgParent; } internal set { imgParent = value; } }

		/// <summary>
		/// The name of the property
		/// </summary>
		public override string Name { get { return name; } set { name = value; } }

		/// <summary>
		/// The WzPropertyType of the property
		/// </summary>
		public override WzPropertyType PropertyType { get { return WzPropertyType.Vector; } }

		public override void WriteValue(WzBinaryWriter writer) {
			writer.WriteStringValue("Shape2D#Vector2D", 0x73, 0x1B);
			writer.WriteCompressedInt(X.Value);
			writer.WriteCompressedInt(Y.Value);
		}

		public override void ExportXml(StreamWriter writer, int level) {
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzVector", Name, false, false) + XmlUtil.Attrib("X", X.Value.ToString()) + XmlUtil.Attrib("Y", Y.Value.ToString(), true, true));
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			name = null;
			x.Dispose();
			x = null;
			y.Dispose();
			y = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// The X value of the Vector2D
		/// </summary>
		public WzCompressedIntProperty X { get { return x; } set { x = value; } }

		/// <summary>
		/// The Y value of the Vector2D
		/// </summary>
		public WzCompressedIntProperty Y { get { return y; } set { y = value; } }

		/// <summary>
		/// The Point of the Vector2D created from the X and Y
		/// </summary>
		public Point Pos { get { return new Point(X.Value, Y.Value); } }

		/// <summary>
		/// Creates a blank WzVectorProperty
		/// </summary>
		public WzVectorProperty() {
		}

		/// <summary>
		/// Creates a WzVectorProperty with the specified name
		/// </summary>
		/// <param name="name">The name of the property</param>
		public WzVectorProperty(string name) {
			this.name = name;
		}

		/// <summary>
		/// Creates a WzVectorProperty with the specified name, x and y
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="x">The x value of the vector</param>
		/// <param name="y">The y value of the vector</param>
		public WzVectorProperty(string name, WzCompressedIntProperty x, WzCompressedIntProperty y) {
			this.name = name;
			this.x = x;
			this.y = y;
		}

		#endregion

		#region Cast Values

		internal override Point ToPoint(int pXDef = 0, int pYDef = 0) {
			return new Point(x.mVal, y.mVal);
		}

		public override string ToString() {
			return "X: " + x.mVal + ", Y: " + y.mVal;
		}

		#endregion
	}
}