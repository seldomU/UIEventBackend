using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EventInspector
{
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

	public struct EventRef
	{
		public delegate IEnumerable<EventRef> GetEventRefs( Object o );
		public delegate UnityEventBase GetValue( Object o );
		public delegate SerializedProperty GetProp( Object o );

		public string name;
		public GetValue getValue;
		public GetProp getProp;
	}

	public class UIEventUtilitly
	{
		// infaces types used by Unity's event system
		public static Type[] EventTypes =
			typeof( IEventSystemHandler )
			.Assembly
			.GetTypes()
			.Where( t => t.IsInterface && typeof( IEventSystemHandler ).IsAssignableFrom( t ) )
			.ToArray();

		static BindingFlags eventFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

		// caching the event list field of EventTrigger to save computation
		static FieldInfo EventTriggerListField = typeof( EventTrigger ).GetField( "m_Delegates", eventFieldFlags );

		public static EventRef.GetEventRefs GetEventRefs( Type componentType )
		{
			if ( componentType == typeof( EventTrigger ) )
			{
				return ( o ) => Enum
					.GetValues( typeof( EventTriggerType ) )
					.Cast<EventTriggerType>()
					.SelectMany( ett => GetTriggerEventRefs( o, ett ) );
			}

			return ( o ) => uiEventFieldMap
				.Where( pair => pair.Key.componentType == componentType )
				.Select( pair => pair.Value )
				.Select( fInfo => GetFieldEventRef( fInfo ) );
		}

		static EventRef GetFieldEventRef( FieldInfo fInfo)
		{
			return new EventRef()
			{
				name = fInfo.Name,
				getValue = ( o ) => (UnityEventBase) fInfo.GetValue( o ),
				getProp = ( o ) => new SerializedObject( o ).FindProperty( fInfo.Name )
			};
		}

		static EventRef[] GetTriggerEventRefs( Object obj, EventTriggerType eventTriggerType )
		{
			var entries = (List<EventTrigger.Entry>) EventTriggerListField.GetValue( obj );

			return Enumerable
				.Range( 0, entries.Count )
				.Where( i => entries[ i ].eventID == eventTriggerType )
				.Select( i => GetTriggerEventRef( entries, i ) )
				.ToArray();
		}

		static EventRef GetTriggerEventRef( List<EventTrigger.Entry> events, int index )
		{
			return new EventRef()
			{
				name =  "Event " + index + ": " + events[index].eventID,	// EventTriggerListField.Name
				getValue = ( o ) => events[ index ].callback,
				getProp = ( o ) => new SerializedObject( o )
					.FindProperty( EventTriggerListField.Name )
					.GetArrayElementAtIndex( index )
					.FindPropertyRelative( "callback" )
			};
		}

		// map pairs of (componentType, eventInterface) to event fieldInfos
		// allowing us to extract the event object from a component
		public static Dictionary<ComponentEventHandler, FieldInfo> uiEventFieldMap =
			new Dictionary<ComponentEventHandler, FieldInfo>()
			{
				{new ComponentEventHandler( typeof( Button ), typeof(IPointerEnterHandler) ), typeof(Button).GetField("m_OnClick", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Button ), typeof(ISubmitHandler) ), typeof(Button).GetField("m_OnClick", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Button ), typeof(IPointerClickHandler) ), typeof(Button).GetField("m_OnClick", eventFieldFlags) },

				{ new ComponentEventHandler( typeof( Slider ), typeof(IDragHandler) ), typeof(Slider).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler( typeof( Toggle ), typeof(IPointerClickHandler) ), typeof(Toggle).GetField("m_OnValueChanged", eventFieldFlags) },
				{new ComponentEventHandler( typeof( Toggle ), typeof(ISubmitHandler) ), typeof(Toggle).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler(typeof(Dropdown), typeof(IPointerEnterHandler) ), typeof(Dropdown).GetField("m_OnValueChanged", eventFieldFlags) },
				{new ComponentEventHandler(typeof(Dropdown), typeof(ISubmitHandler) ), typeof(Dropdown).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler(typeof(Scrollbar), typeof(IDragHandler) ), typeof(Scrollbar).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler(typeof(ScrollRect), typeof(IScrollHandler) ), typeof(ScrollRect).GetField("m_OnValueChanged", eventFieldFlags) },
				{ new ComponentEventHandler(typeof(ScrollRect), typeof(IDragHandler) ), typeof(ScrollRect).GetField("m_OnValueChanged", eventFieldFlags) },
				{ new ComponentEventHandler(typeof(ScrollRect), typeof(IEndDragHandler) ), typeof(ScrollRect).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler(typeof(InputField), typeof(IPointerClickHandler) ), typeof(InputField).GetField("m_OnValueChanged", eventFieldFlags) },
				{ new ComponentEventHandler(typeof(InputField), typeof(ISubmitHandler) ), typeof(InputField).GetField("m_OnValueChanged", eventFieldFlags) },

				{ new ComponentEventHandler(typeof(InputField), typeof(IUpdateSelectedHandler) ), typeof(InputField).GetField("m_EndEdit", eventFieldFlags) },
				{ new ComponentEventHandler(typeof(InputField), typeof(ISelectHandler) ), typeof(InputField).GetField("m_EndEdit", eventFieldFlags) },
				{ new ComponentEventHandler(typeof(InputField), typeof(IDeselectHandler) ), typeof(InputField).GetField("m_EndEdit", eventFieldFlags) }

				// ignore these components that consume UI events but don't allow the user to add listeners
				// Selectable: IMoveHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
				// Dropdown: ICancelHandler
				// DropdownItem: IPointerEnterHandler, ICancelHandler
			};
	}
}
