using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace WasmMT.Wasm
{
    public class BindableMetadataProvider : global::Uno.UI.DataBinding.IBindableMetadataProvider
    {
        public IBindableType GetBindableTypeByFullName(string fullName)
        {
            return null;
        }

        public IBindableType GetBindableTypeByType(Type type)
        {
            return null;
        }
    }
}