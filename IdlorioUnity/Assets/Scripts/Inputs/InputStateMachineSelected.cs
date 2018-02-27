﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputStateMachineSelected : BaseInputState
{
    private Machine _selectedMachine = null;

    public InputStateMachineSelected(Machine machine)
    {
        _selectedMachine = machine;
    }

    public override void OnEnter()
    {
        base.OnEnter();

        if (_selectedMachine != null)
        {
            _selectedMachine._isSelected = true;
            UiManager.GetInstance().EnableMachineSelectedPanel(_selectedMachine.GetMachineInfoString());
            UiManager.GetInstance().EnableSelectionOptionsPanel();
        }
    }

    public override void OnExit()
    {
        base.OnExit();

        if (_selectedMachine != null)
        {
            _selectedMachine._isSelected = false;
        }

        UiManager.GetInstance().DisableMachineSelectedPanel();
    }

    protected override void OnSubstateExit()
    {
        base.OnSubstateExit();
        
        UiManager.GetInstance().EnableSelectionOptionsPanel();
    }

    public override void Update()
    {
        base.Update();

        if (_selectedMachine == null)
        {
            TerminateState();
        }
    }

    protected override bool OnCursorClickInternal(Point tile)
    {
        Machine machine = MachineManager.GetInstance().GetMachineAtPoint(tile);

        if (machine == _selectedMachine)
        {
            TerminateState();
            return true;
        }

        return false;
    }

    protected override bool OnButtonClickedInternal(ButtonType button)
    {
        if (button == ButtonType.Move)
        {
            SetSubstate(new InputStateMove(_selectedMachine));
            UiManager.GetInstance().DisableSelectionOptionsPanel();
            return true;
        }

        return false;
    }
}
