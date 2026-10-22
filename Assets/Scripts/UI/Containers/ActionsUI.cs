using System.Collections.Generic;
using System.Linq;
using Commands;
using EventBus;
using Events;
using UI.Components;
using Units;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Containers
{
    public class ActionsUI : MonoBehaviour, IUIElement<HashSet<AbstractCommandable>>
    {
        [SerializeField] private UIActionButton[] actionButtons;
        private HashSet<AbstractCommandable> selectedUnits = new(12);

        public void EnableFor(HashSet<AbstractCommandable> selectedUnits)
        {
            RefreshButtons(selectedUnits);
        }

        public void Disable()
        {
            foreach(UIActionButton button in actionButtons)
            {
                button.Disable();
            }
        }

        private void Start()
        {
            foreach(UIActionButton actionButton in actionButtons)
            {
                actionButton.Disable();
            }   
        }

        private void RefreshButtons(HashSet<AbstractCommandable> selectedUnits)
        {
            HashSet<ActionBase> availableCommands = new(9);

            foreach (AbstractCommandable commandable in selectedUnits)
            {
                availableCommands.AddRange(commandable.AvailableCommands);
            }

            for (int i = 0; i < actionButtons.Length; i++)
            {
                ActionBase actionForSlot = availableCommands.Where(action => action.Slot == i).FirstOrDefault();

                if (actionForSlot != null)
                {
                    actionButtons[i].EnableFor(actionForSlot, HandleClick(actionForSlot));
                }
                else
                {
                    actionButtons[i].Disable();
                }
            }
        }

        private UnityAction HandleClick(ActionBase action)
        {
            return () => Bus<ActionSelectedEvent>.Raise(new ActionSelectedEvent(action));
        }

    }
}