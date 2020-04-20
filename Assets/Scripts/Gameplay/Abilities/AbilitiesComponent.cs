﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitiesComponent : MonoBehaviour
{
    public AbilitySpritesDB sprites;
    public AbilityResources resources;

    public TargetObject DirectionTargetPrefab;
    public TargetObject PositionTargetPrefab;
    public TargetObject ObjectTargetPrefab;

    /// <summary> Activates the ability at the given slot </summary>
    /// <returns> The required targeting of the ability </returns>
    public void ActivateAbility(int slotIndex)
    {
        abilityHeldTimer[slotIndex] = 0;

        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        AbilitySlot slot = abilitySlots[slotIndex];
        slot.Activate();

        switch(slot.targeting)
        {
            case AbilityTargeting.Line:
            case AbilityTargeting.Cone:
                slot.targetObject = CreateDirectionTarget();
                break;

            case AbilityTargeting.Area:
                slot.targetObject = CreatePositionTarget();
                break;

            case AbilityTargeting.Unit:
                slot.targetObject = CreateObjectTarget();
                break;
        }
    }

    public void ShuffleAbility(int slotIndex)
    {
        abilityHeldTimer[slotIndex] = 0;

        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        AbilitySlot slot = abilitySlots[slotIndex];
        slot.ClearForShuffle();
    }
    
    /// <summary> Draws a random ability from the deck into the given slot </summary>
    public bool DrawAbility(int slotIndex)
    {
        return DrawAbility(GetSlot(slotIndex));
    }

    /// <summary> Draws a random ability from the deck into the given slot </summary>
    public bool DrawAbility(AbilitySlot slot)
    {
        if (slot!=null)
        {
            if (slot.ability == null)
            {
                int x = Service.Grid.Columns / 2;
                int y = Service.Grid.Rows / 2;

                var actorComp = GetComponent<GridActor>();
                if (actorComp)
                {
                    x = actorComp.TargetPosition.x;
                    y = actorComp.TargetPosition.y;
                }

                slot.ability = abilityDeck.Draw(abilitySlots, x, y);
                return true;
            }
        }

        return false;
    }

    /// <returns> The the ability sprite </returns>
    public Sprite GetAbilitySprite(int slotIndex)
    {
        AbilityBase ability = GetAbility(slotIndex);
        return ability != null ? ability.sprite : sprites.Empty;
    }

    /// <returns> The the ability name </returns>
    public string GetAbilityName(int slotIndex)
    {
        return GetAbility(slotIndex)?.name;
    }

    /// <returns> The the ability cooldown progress (from 1 cooldown started; to 1 no cooldown)  </returns>
    public float GetCooldownProgress(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot != null)
        {
            return 1.0f - slot.cooldownTimer / (slot.WasJustShuffled ? AbilitySlot.SHUFFLE_COOLDOWN_TIME : AbilitySlot.COOLDOWN_TIME);
        }

        return 1.0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetupDeck();
        SetupSlots();
    }

    public static string GetInputKey(int slot)
    {
        return $"Ability{slot+1}";
    }

    public bool NeedsToLiftKey(int slot)
    {
        return abilityNeedsKeyLift[slot];
    }

    void ProcessInputSlotShuffle(int slot)
    {
        bool keyDown = Input.GetButton(GetInputKey(slot));

        if (keyDown)
        {
            if (!abilityNeedsKeyLift[slot])
            {
                abilityHeldTimer[slot] += Time.deltaTime;
                if (abilityHeldTimer[slot] >= AbilitySlot.HOLD_TO_SHUFFLE_TIME)
                {
                    ShuffleAbility(slot);
                    abilityNeedsKeyLift[slot] = true;
                }
            }
        }
        else if(abilityNeedsKeyLift[slot])
        {
            abilityHeldTimer[slot] = 0;
            abilityNeedsKeyLift[slot] = false;
        }
    }

    void ProcessInputSlot(int slot)
    {
        if (!abilityNeedsKeyLift[slot] && Input.GetButtonUp(GetInputKey(slot)))
        {
            ActivateAbility(slot);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!CompareTag("Player"))
        {
            return;
        }

        foreach (AbilitySlot slot in abilitySlots)
        {
            slot?.Update(Time.deltaTime);
        }

        for (int i = 0; i < NUM_SLOTS; i++)
        {
            ProcessInputSlot(i);
            ProcessInputSlotShuffle(i);
        }
    }

    void SetupSlots()
    {
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            abilitySlots[i] = new AbilitySlot(gameObject, i);
            abilitySlots[i].OnCooldownEnded += slot => DrawAbility(slot);
            abilitySlots[i].Clear(true);
            //DrawAbility(abilitySlots[i]);
        };
    }

    void SetupDeck()
    {
        // Add abilities
        // abilityDeck.Add( new Ability...() );                

        abilityDeck.Add(new MoveAbility("Move Left", sprites.MoveLeft, AbilityTargeting.None, -1, 0), 5);
        abilityDeck.Add(new MoveAbility("Move Right", sprites.MoveRight, AbilityTargeting.None, 1, 0), 5);
        abilityDeck.Add(new MoveAbility("Move Forward", sprites.MoveForward, AbilityTargeting.None, 0, 1));
        abilityDeck.Add(new MoveAbility("Move Back", sprites.MoveBack, AbilityTargeting.None, 0, -1));

        abilityDeck.Add(new HealAbility("Heal", sprites.Heal, 35), 3);
        abilityDeck.Add(new SpreadshotAbility("Spread Shot", sprites.SpreadShot, resources.BulletPrefab), 4);
    }

    bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < abilitySlots.Length;
    }

    AbilitySlot GetSlot(int slotIndex)
    {
        if (IsValidSlotIndex(slotIndex))
        {
            return abilitySlots[slotIndex];
        }

        return null;
    }

    AbilityBase GetAbility(int slotIndex)
    {
        return GetSlot(slotIndex)?.ability;
    }

    TargetObject CreateDirectionTarget()
    {
        return DirectionTargetPrefab?.CreateFor(gameObject);
    }

    TargetObject CreatePositionTarget()
    {
        return PositionTargetPrefab?.CreateFor(gameObject);
    }

    TargetObject CreateObjectTarget()
    {
        return ObjectTargetPrefab?.CreateFor(gameObject);
    }

    private const int NUM_SLOTS = 5;

    AbilitySlot[] abilitySlots      = new AbilitySlot[NUM_SLOTS];
    float[] abilityHeldTimer        = new float[NUM_SLOTS];
    bool[] abilityNeedsKeyLift      = new bool[NUM_SLOTS];
    AbilityDeck abilityDeck         = new AbilityDeck();
}
