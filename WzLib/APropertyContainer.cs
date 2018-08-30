using MapleLib.WzLib.WzProperties;
using System.Collections.Generic;

namespace MapleLib.WzLib {
    public abstract class APropertyContainer : AWzImageProperty {
        public virtual void AddProperty(AWzImageProperty pProp) {
            pProp.Parent = this;
            pProp.ParentImage = ParentImage;
            WzProperties.Add(pProp);
        }

        public virtual void AddProperties(List<AWzImageProperty> pProps) {
            foreach (AWzImageProperty prop in pProps) {
                AddProperty(prop);
            }
            pProps.Clear();
        }

        public virtual void RemoveProperty(AWzImageProperty pProp) {
            WzProperties.Remove(pProp);
        }

        public virtual void ClearProperties() {
            WzProperties.Clear();
        }

        public override AWzImageProperty this[string pName] {
            get {
                foreach (AWzImageProperty prop in WzProperties)
                    if (pName == "PNG" && prop is WzCanvasProperty) {
                        return prop.ToPngProperty();
                    } else if (prop.Name.ToLower() == pName.ToLower()) {
                        return prop;
                    }
                return null;
            }
        }
    }
}