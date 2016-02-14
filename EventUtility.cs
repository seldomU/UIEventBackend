using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEditor;

namespace EventInspector
{
	// UnityEvent listener information
	public class ListenerData
	{
		public Object target;
		public string method;
		public ListenerCallArgument argument;

		public bool IsValid()
		{
			return target != null;
		}

		public override string ToString() { return "( " +target.ToString() + ", " + method + ", " + argument.ToString() + " )"; }
	}

	// the argument of a UnityEvent listener call
	public class ListenerCallArgument
	{
		public PersistentListenerMode mode;
		public Object objValue;
		public int intValue;
		public float floatValue;
		public string stringValue;
		public bool boolValue;

		public override string ToString()
		{
			switch ( mode ) {
				case PersistentListenerMode.Object:
					return "Object: " + objValue;

				case PersistentListenerMode.Int:
					return "Int: " + intValue;

				case PersistentListenerMode.Float:
					return "Float: " + floatValue;

				case PersistentListenerMode.String:
					return "String: " + stringValue;

				case PersistentListenerMode.Bool:
					return "Bool: " + boolValue;

				case PersistentListenerMode.Void:
				case PersistentListenerMode.EventDefined:
					return "Void";

				default:
					throw new System.Exception( "unhandled mode " + mode );
			}
		}
	}

	public class EventUtility
	{
		// extracts the data of all listeners of the event.
		public static IEnumerable<ListenerData> GetListenerData( UnityEventBase ev, SerializedProperty eventProp )
		{
			int numListeners = ev.GetPersistentEventCount();
			for ( int i = 0; i < numListeners; i++ )
			{
				Object target = ev.GetPersistentTarget( i );
				string method = ev.GetPersistentMethodName( i );
				ListenerCallArgument methodArg = GetEventArgumentObject( eventProp, i );
				yield return new ListenerData()
				{
					target = target,
					method = method,
					argument = methodArg
				};
			}
		}

		// Unity's API does not provide access to event arguments. this is a workaround
		public static ListenerCallArgument GetEventArgumentObject( SerializedProperty eventProp, int index )
		{
			var argument = new ListenerCallArgument();

			var persCallsProp = eventProp.FindPropertyRelative( "m_PersistentCalls" );
			var callsProp = persCallsProp.FindPropertyRelative( "m_Calls" );
			var callProp = callsProp.GetArrayElementAtIndex( index );
			var callArgProp = callProp.FindPropertyRelative( "m_Arguments" );
			var modeProp = callProp.FindPropertyRelative( "m_Mode" );
			argument.mode = (PersistentListenerMode) modeProp.enumValueIndex;
			switch ( argument.mode )
			{
				case PersistentListenerMode.Object:
					var objectArgProp = callArgProp.FindPropertyRelative( "m_ObjectArgument" );
					argument.objValue = objectArgProp.objectReferenceValue;
					//var objectArgTypeName = callArgProp.FindPropertyRelative( "m_ObjectArgumentAssemblyTypeName" );
					break;
				case PersistentListenerMode.Int:
					argument.intValue = callArgProp.FindPropertyRelative( "m_IntArgument" ).intValue;
					break;
				case PersistentListenerMode.Float:
					argument.floatValue = callArgProp.FindPropertyRelative( "m_FloatArgument" ).floatValue;
					break;
				case PersistentListenerMode.Bool:
					argument.boolValue = callArgProp.FindPropertyRelative( "m_BoolArgument" ).boolValue;
					break;
				case PersistentListenerMode.String:
					argument.stringValue = callArgProp.FindPropertyRelative( "m_StringArgument" ).stringValue;
					break;
				case PersistentListenerMode.Void:
				case PersistentListenerMode.EventDefined:
					// do nothing
					break;
				default:
					throw new System.Exception( "unhandled mode: " + argument.mode );
			}
			return argument;
		}
	}
}
