// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — UiaDefinitions.cs
// Minimal COM interop interfaces for UI Automation focus tracking.
// ──────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;

namespace YangiAlifbo.Win32;

[ComImport]
[Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
internal class CUIAutomation
{
}

[ComImport]
[Guid("30cbe57d-d9d0-452a-ab13-7ac0e10243fe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomation
{
    [PreserveSig] int CompareElements(IntPtr el1, IntPtr el2, out int areSame);
    [PreserveSig] int CompareRuntimeIds(IntPtr r1, IntPtr r2, out int areSame);
    [PreserveSig] int GetRootElement(out IntPtr root);
    [PreserveSig] int ElementFromHandle(IntPtr hwnd, out IntPtr element);
    [PreserveSig] int ElementFromPoint(NativeMethods.POINT pt, out IntPtr element);
    [PreserveSig] int GetFocusedElement(out IntPtr element);
    [PreserveSig] int GetRootElementBuildCache(IntPtr request, out IntPtr root);
    [PreserveSig] int ElementFromHandleBuildCache(IntPtr hwnd, IntPtr request, out IntPtr element);
    [PreserveSig] int ElementFromPointBuildCache(NativeMethods.POINT pt, IntPtr request, out IntPtr element);
    [PreserveSig] int GetFocusedElementBuildCache(IntPtr request, out IntPtr element);
    [PreserveSig] int CreateTreeWalker(IntPtr pCondition, out IntPtr walker);
    [PreserveSig] int GetControlViewWalker(out IntPtr walker);
    [PreserveSig] int GetContentViewWalker(out IntPtr walker);
    [PreserveSig] int GetRawViewWalker(out IntPtr walker);
    [PreserveSig] int GetRawViewCondition(out IntPtr condition);
    [PreserveSig] int GetControlViewCondition(out IntPtr condition);
    [PreserveSig] int GetContentViewCondition(out IntPtr condition);
    [PreserveSig] int CreatePropertyCondition(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value, out IntPtr condition);
    [PreserveSig] int CreatePropertyConditionEx(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value, int flags, out IntPtr condition);
    [PreserveSig] int CreateAndCondition(IntPtr c1, IntPtr c2, out IntPtr condition);
    [PreserveSig] int CreateAndConditionFromArray(IntPtr conditions, out IntPtr condition);
    [PreserveSig] int CreateAndConditionFromNativeArray(IntPtr conditions, int conditionCount, out IntPtr condition);
    [PreserveSig] int CreateOrCondition(IntPtr c1, IntPtr c2, out IntPtr condition);
    [PreserveSig] int CreateOrConditionFromArray(IntPtr conditions, out IntPtr condition);
    [PreserveSig] int CreateOrConditionFromNativeArray(IntPtr conditions, int conditionCount, out IntPtr condition);
    [PreserveSig] int CreateNotCondition(IntPtr c, out IntPtr condition);
    [PreserveSig] int AddAutomationEventHandler(int eventId, IntPtr element, int scope, IntPtr cacheRequest, IntPtr handler);
    [PreserveSig] int RemoveAutomationEventHandler(int eventId, IntPtr element, IntPtr handler);
    [PreserveSig] int AddPropertyChangedEventHandlerNativeArray(IntPtr element, int scope, IntPtr cacheRequest, IntPtr handler, IntPtr propertyArray, int propertyCount);
    [PreserveSig] int AddPropertyChangedEventHandler(IntPtr element, int scope, IntPtr cacheRequest, IntPtr handler, IntPtr propertyArray);
    [PreserveSig] int RemovePropertyChangedEventHandler(IntPtr element, IntPtr handler);
    [PreserveSig] int AddStructureChangedEventHandler(IntPtr element, int scope, IntPtr cacheRequest, IntPtr handler);
    [PreserveSig] int RemoveStructureChangedEventHandler(IntPtr element, IntPtr handler);
    [PreserveSig] int AddFocusChangedEventHandler(IntPtr cacheRequest, [In, MarshalAs(UnmanagedType.Interface)] IUIAutomationFocusChangedEventHandler handler);
    [PreserveSig] int RemoveFocusChangedEventHandler([In, MarshalAs(UnmanagedType.Interface)] IUIAutomationFocusChangedEventHandler handler);
}

[ComImport]
[Guid("c2531221-bf61-4122-9c32-b28bc64f1134")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationFocusChangedEventHandler
{
    [PreserveSig] int HandleFocusChangedEvent([In, MarshalAs(UnmanagedType.Interface)] IUIAutomationElement sender);
}

[ComImport]
[Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElement
{
    [PreserveSig] int SetFocus();
    [PreserveSig] int GetRuntimeId(out IntPtr runtimeId);
    [PreserveSig] int FindFirst(int scope, IntPtr condition, out IntPtr found);
    [PreserveSig] int FindAll(int scope, IntPtr condition, out IntPtr found);
    [PreserveSig] int FindFirstBuildCache(int scope, IntPtr condition, IntPtr cacheRequest, out IntPtr found);
    [PreserveSig] int FindAllBuildCache(int scope, IntPtr condition, IntPtr cacheRequest, out IntPtr found);
    [PreserveSig] int BuildUpdatedCache(IntPtr cacheRequest, out IntPtr updatedElement);
    [PreserveSig] int GetCurrentPropertyValue(int propertyId, [MarshalAs(UnmanagedType.Struct)] out object value);
}
