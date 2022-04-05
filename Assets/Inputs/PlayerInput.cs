// GENERATED AUTOMATICALLY FROM 'Assets/Inputs/PlayerInput.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Overrailed.Player
{
    public class @PlayerInput : IInputActionCollection, IDisposable
    {
        public InputActionAsset asset { get; }
        public @PlayerInput()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerInput"",
    ""maps"": [
        {
            ""name"": ""Movement"",
            ""id"": ""7f88ce00-e1d5-4d95-b6e8-0eee45a9f816"",
            ""actions"": [
                {
                    ""name"": ""Walk"",
                    ""type"": ""Value"",
                    ""id"": ""aac96094-88c6-47fe-9fc9-95ba4e222dc0"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Dash"",
                    ""type"": ""Button"",
                    ""id"": ""1ba1df66-fa46-49b4-9ac8-6bf9a6b37172"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keys"",
                    ""id"": ""1503b38f-d81e-4745-834e-38adf10360e8"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d4fcf1ed-0272-47ea-95fc-f162265e1c6c"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""a4859c18-7cbd-403a-b773-3e3c80ab32bd"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""ad5ab61e-262f-4870-90b3-b3594694630e"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""36a9b8e3-a8a0-409b-9cc1-2af3e00c0429"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""40a7df88-aa62-40fd-a376-9f2c0bdf7b14"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""91eff4e9-79f9-4b9a-9495-472ffced10be"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Dash"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a8f5579f-dece-440a-b0fb-f12e2e1647de"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Dash"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Interaction"",
            ""id"": ""e7b10c03-e498-4779-9afa-0b3af5c74461"",
            ""actions"": [
                {
                    ""name"": ""Interact Main"",
                    ""type"": ""Button"",
                    ""id"": ""e6775c5a-b5a5-45f7-abc5-6578e84283bf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Interact Alt"",
                    ""type"": ""Button"",
                    ""id"": ""0097aeac-8909-4363-a548-1370cf654346"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f5e1907f-090b-4e9b-acf5-731d6b17c59c"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact Main"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9659771e-81e9-4910-af8c-6f802e6cf74b"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact Main"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1714c961-79ef-4d5d-8a26-5719a0e2a7f5"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact Alt"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6ceb7794-d407-4294-8905-bc109086ed72"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact Alt"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // Movement
            m_Movement = asset.FindActionMap("Movement", throwIfNotFound: true);
            m_Movement_Walk = m_Movement.FindAction("Walk", throwIfNotFound: true);
            m_Movement_Dash = m_Movement.FindAction("Dash", throwIfNotFound: true);
            // Interaction
            m_Interaction = asset.FindActionMap("Interaction", throwIfNotFound: true);
            m_Interaction_InteractMain = m_Interaction.FindAction("Interact Main", throwIfNotFound: true);
            m_Interaction_InteractAlt = m_Interaction.FindAction("Interact Alt", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        // Movement
        private readonly InputActionMap m_Movement;
        private IMovementActions m_MovementActionsCallbackInterface;
        private readonly InputAction m_Movement_Walk;
        private readonly InputAction m_Movement_Dash;
        public struct MovementActions
        {
            private @PlayerInput m_Wrapper;
            public MovementActions(@PlayerInput wrapper) { m_Wrapper = wrapper; }
            public InputAction @Walk => m_Wrapper.m_Movement_Walk;
            public InputAction @Dash => m_Wrapper.m_Movement_Dash;
            public InputActionMap Get() { return m_Wrapper.m_Movement; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(MovementActions set) { return set.Get(); }
            public void SetCallbacks(IMovementActions instance)
            {
                if (m_Wrapper.m_MovementActionsCallbackInterface != null)
                {
                    @Walk.started -= m_Wrapper.m_MovementActionsCallbackInterface.OnWalk;
                    @Walk.performed -= m_Wrapper.m_MovementActionsCallbackInterface.OnWalk;
                    @Walk.canceled -= m_Wrapper.m_MovementActionsCallbackInterface.OnWalk;
                    @Dash.started -= m_Wrapper.m_MovementActionsCallbackInterface.OnDash;
                    @Dash.performed -= m_Wrapper.m_MovementActionsCallbackInterface.OnDash;
                    @Dash.canceled -= m_Wrapper.m_MovementActionsCallbackInterface.OnDash;
                }
                m_Wrapper.m_MovementActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @Walk.started += instance.OnWalk;
                    @Walk.performed += instance.OnWalk;
                    @Walk.canceled += instance.OnWalk;
                    @Dash.started += instance.OnDash;
                    @Dash.performed += instance.OnDash;
                    @Dash.canceled += instance.OnDash;
                }
            }
        }
        public MovementActions @Movement => new MovementActions(this);

        // Interaction
        private readonly InputActionMap m_Interaction;
        private IInteractionActions m_InteractionActionsCallbackInterface;
        private readonly InputAction m_Interaction_InteractMain;
        private readonly InputAction m_Interaction_InteractAlt;
        public struct InteractionActions
        {
            private @PlayerInput m_Wrapper;
            public InteractionActions(@PlayerInput wrapper) { m_Wrapper = wrapper; }
            public InputAction @InteractMain => m_Wrapper.m_Interaction_InteractMain;
            public InputAction @InteractAlt => m_Wrapper.m_Interaction_InteractAlt;
            public InputActionMap Get() { return m_Wrapper.m_Interaction; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(InteractionActions set) { return set.Get(); }
            public void SetCallbacks(IInteractionActions instance)
            {
                if (m_Wrapper.m_InteractionActionsCallbackInterface != null)
                {
                    @InteractMain.started -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractMain;
                    @InteractMain.performed -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractMain;
                    @InteractMain.canceled -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractMain;
                    @InteractAlt.started -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractAlt;
                    @InteractAlt.performed -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractAlt;
                    @InteractAlt.canceled -= m_Wrapper.m_InteractionActionsCallbackInterface.OnInteractAlt;
                }
                m_Wrapper.m_InteractionActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @InteractMain.started += instance.OnInteractMain;
                    @InteractMain.performed += instance.OnInteractMain;
                    @InteractMain.canceled += instance.OnInteractMain;
                    @InteractAlt.started += instance.OnInteractAlt;
                    @InteractAlt.performed += instance.OnInteractAlt;
                    @InteractAlt.canceled += instance.OnInteractAlt;
                }
            }
        }
        public InteractionActions @Interaction => new InteractionActions(this);
        public interface IMovementActions
        {
            void OnWalk(InputAction.CallbackContext context);
            void OnDash(InputAction.CallbackContext context);
        }
        public interface IInteractionActions
        {
            void OnInteractMain(InputAction.CallbackContext context);
            void OnInteractAlt(InputAction.CallbackContext context);
        }
    }
}