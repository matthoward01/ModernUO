using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.PlayerMurderSystem;

[SerializationGenerator(0)]
public partial class MurderContext
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _shortTermElapse = TimeSpan.MaxValue;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _longTermElapse = TimeSpan.MaxValue;

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int ShortTermMurders
    {
        get => _shortTermMurders;
        set => _shortTermMurders = Math.Max(value, 0);
    }

    [DirtyTrackingEntity]
    public PlayerMobile _player;

    public PlayerMobile Player => _player;

    // Wall clock time for next short or long term expiration
    internal DateTime _nextElapse;

    public MurderContext(PlayerMobile player) => _player = player;

    public void ResetKillTime(bool isShort = true, bool isLong = true)
    {
        var gameTime = _player.GameTime;

        if (isShort)
        {
            ShortTermElapse = gameTime + PlayerMurderSystem.ShortTermMurderDuration;
        }

        if (isLong)
        {
            LongTermElapse = gameTime + PlayerMurderSystem.LongTermMurderDuration;
        }
    }

    public void DecayKills()
    {
        var gameTime = _player.GameTime;

        if (ShortTermElapse < gameTime)
        {
            ShortTermElapse += PlayerMurderSystem.ShortTermMurderDuration;
            if (ShortTermMurders > 0)
            {
                --ShortTermMurders;
            }
        }

        if (LongTermElapse < gameTime)
        {
            LongTermElapse += PlayerMurderSystem.LongTermMurderDuration;
            if (_player.Kills > 0)
            {
                --_player.Kills;
            }
        }
    }

    public bool CheckStart()
    {
        _nextElapse = DateTime.MaxValue;

        var now = Core.Now;
        var gameTime = _player.GameTime;

        if (ShortTermMurders > 0)
        {
            _nextElapse = now + (ShortTermElapse - gameTime);
        }

        if (_player.Kills > 0)
        {
            var timeUntilLong = now + (LongTermElapse - gameTime);
            if (_nextElapse > timeUntilLong)
            {
                _nextElapse = timeUntilLong;
            }
        }

        return _nextElapse != DateTime.MaxValue;
    }

    public class EqualityComparer : IEqualityComparer<MurderContext>
    {
        public static EqualityComparer Default { get; } = new ();

        public bool Equals(MurderContext x, MurderContext y) => x?._player == y?._player;

        public int GetHashCode(MurderContext context) => context._player?.GetHashCode() ?? 0;
    }
}
