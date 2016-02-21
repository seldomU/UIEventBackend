using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

	// event data needed for UI and extracting listener info 
	public struct EventRef
	{
		public string name;
		public UnityEventBase value;
		public SerializedProperty prop;
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

		// returns EventRefs for all UI events exposed by the component
		public static IEnumerable<EventRef> GetEventRefs( Component component )
		{
			// EventTriggers store their events in a list
			var asEventTrigger = component as EventTrigger;
			if ( asEventTrigger != null )
			{
				return Enum
					.GetValues( typeof( EventTriggerType ) )
					.Cast<EventTriggerType>()
					.SelectMany( ett => GetTriggerEventRefs( asEventTrigger, ett ) );
			}

			// all others UI components have one field per event
			var compType = component.GetType();
			return uiEventFieldMap
				.Where( pair => pair.Key.componentType == compType )
				.Select( pair => pair.Value )
				.Select( fInfo => GetFieldEventRef( fInfo, component ) );
		}

		// returns EventRef for the component's given event field
		static EventRef GetFieldEventRef( FieldInfo fInfo, Component component)
		{
			return new EventRef()
			{
				name = fInfo.Name,
				value = (UnityEventBase) fInfo.GetValue( component ),
				prop = new SerializedObject( component ).FindProperty( fInfo.Name )
			};
		}

		// returns EventRefs for those entries in the trigger's event list that match the given type
		static EventRef[] GetTriggerEventRefs( EventTrigger eventTrigger, EventTriggerType eventTriggerType )
		{
			// extract the list field value
			var entries = (List<EventTrigger.Entry>) EventTriggerListField.GetValue( eventTrigger );

			// filter the list by event type and convert it to EventRefs
			return Enumerable
				.Range( 0, entries.Count )
				.Where( i => entries[ i ].eventID == eventTriggerType )
				.Select( i => GetTriggerEventRef( entries, i, eventTrigger ) )
				.ToArray();
		}

		// convert a list entry to EventRef
		static EventRef GetTriggerEventRef( List<EventTrigger.Entry> events, int index, Component component )
		{
			return new EventRef()
			{
				name =  "Event " + index + ": " + events[index].eventID,	// EventTriggerListField.Name
				value = events[ index ].callback,
				prop = new SerializedObject( component )
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
