using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EventInspector
{
	class UIEventUtilitly
	{
		public static Type[] EventTypes =
			typeof( IEventSystemHandler )
			.Assembly
			.GetTypes()
			.Where( t => t.IsInterface && typeof( IEventSystemHandler ).IsAssignableFrom( t ) )
			.ToArray();

		// just a tuple of (component type, UI event interface)
		public struct ComponentEventHandler
		{
			public Type componentType;
			public Type eventHandlerType;

			public ComponentEventHandler( Type componentType, Type eventHandlerType )
			{
				this.componentType = componentType;
				this.eventHandlerType = eventHandlerType;
			}
		}

		static BindingFlags eventFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

		// map pairs of (componentType, eventInterface) to event fieldInfos
		// allowing us to extract the event value
		public static Dictionary<ComponentEventHandler, FieldInfo> uiEventFieldMap =
			new Dictionary<ComponentEventHandler, FieldInfo>()
			{
				{new ComponentEventHandler( typeof( Button ), typeof(ISubmitHandler) ), typeof(Button).GetField("m_OnClick", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Button ), typeof(IPointerClickHandler) ), typeof(Button).GetField("m_OnClick", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Slider ), typeof(IDragHandler) ), typeof(Slider).GetField("m_OnValueChanged", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Toggle ), typeof(IPointerClickHandler) ), typeof(Toggle).GetField("m_OnValueChanged", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Toggle ), typeof(ISubmitHandler) ), typeof(Toggle).GetField("m_OnValueChanged", eventFieldFlags) },
			};

		//	todo: Find the component types that contain events of these types: TriggerEvent, ColorTweenCallback, FloatTweenCallback, ButtonClickedEvent, DropdownEvent, SubmitEvent, OnChangeEvent, CullStateChangedEvent, ScrollEvent, ScrollRectEvent, SliderEvent, ToggleEvent
		// - EventTrigger
		// map them to the field
	}
}
