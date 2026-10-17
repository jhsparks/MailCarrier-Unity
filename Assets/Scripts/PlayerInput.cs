using UnityEngine;

// Per-player keyboard layouts for shared-keyboard multiplayer. Slot -1 uses the original solo controls.
public static class PlayerInput
{
    struct Scheme
    {
        public KeyCode up, down, left, right, run, action, interact, horn;
        public string description;
    }

    static readonly Scheme[] schemes =
    {
        new Scheme { up = KeyCode.W, down = KeyCode.S, left = KeyCode.A, right = KeyCode.D, run = KeyCode.LeftShift, action = KeyCode.Space, interact = KeyCode.E, horn = KeyCode.H,
            description = "WASD move | Shift run | Space action | E van | H horn" },
        new Scheme { up = KeyCode.UpArrow, down = KeyCode.DownArrow, left = KeyCode.LeftArrow, right = KeyCode.RightArrow, run = KeyCode.RightShift, action = KeyCode.Return, interact = KeyCode.RightControl, horn = KeyCode.Slash,
            description = "Arrows move | R-Shift run | Enter action | R-Ctrl van | / horn" },
        new Scheme { up = KeyCode.I, down = KeyCode.K, left = KeyCode.J, right = KeyCode.L, run = KeyCode.U, action = KeyCode.O, interact = KeyCode.P, horn = KeyCode.Y,
            description = "IJKL move | U run | O action | P van | Y horn" },
        new Scheme { up = KeyCode.Keypad8, down = KeyCode.Keypad5, left = KeyCode.Keypad4, right = KeyCode.Keypad6, run = KeyCode.Keypad0, action = KeyCode.KeypadEnter, interact = KeyCode.KeypadPlus, horn = KeyCode.KeypadMinus,
            description = "Numpad 8456 move | 0 run | Enter action | + van | - horn" },
    };

    public static string Describe(int slot)
    {
        return schemes[Mathf.Clamp(slot, 0, schemes.Length - 1)].description;
    }

    public static float Horizontal(int slot)
    {
        Scheme s = schemes[slot];
        return (Input.GetKey(s.right) ? 1f : 0f) - (Input.GetKey(s.left) ? 1f : 0f);
    }

    public static float Vertical(int slot)
    {
        Scheme s = schemes[slot];
        return (Input.GetKey(s.up) ? 1f : 0f) - (Input.GetKey(s.down) ? 1f : 0f);
    }

    public static bool Run(int slot) { return Input.GetKey(schemes[slot].run); }
    public static bool ActionDown(int slot) { return Input.GetKeyDown(schemes[slot].action); }
    public static bool InteractDown(int slot) { return Input.GetKeyDown(schemes[slot].interact); }
    public static bool HornDown(int slot) { return Input.GetKeyDown(schemes[slot].horn); }
}
