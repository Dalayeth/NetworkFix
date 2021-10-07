A simple mod that allows you to configure the packet queue size and bandwidth. Should make multiplayer less laggy.

This mod uses a transpiler patch which allows you to modify sendQueueSize, hard-coded to 10,240. as well as a postfix patch which allows you to modify the SendRateMin and SendRateMax variables. This should allow your server to trasmit more than 150kbps, (153,600bps). I've also removed the 512kbps, (524,288bps) maximum buffer size from the socket allowing you to set it to an arbitrary limit.

These changes were first discovered to be helpful [here](https://jamesachambers.com/revisiting-fixing-valheim-lag-modifying-send-receive-limits/) and expanded upon in this [comment chain](https://jamesachambers.com/revisiting-fixing-valheim-lag-modifying-send-receive-limits/#comment-11709). I only made what they were doing manually into a mod to make it easier to use. Shout out to all their great detective work. Special shout-out Patk88 for the config defaults.

If you have any issues with this mod please post them to [https://github.com/Dalayeth/NetworkFix/issues](https://github.com/Dalayeth/NetworkFix/issues)

Changelog:
```
1.1.0
	***It is recommended you delete your config for version 1.1.0***
	Changed from modifying RegisterGlobalCallbacks function with a transpilier to a postfix.
	Allowed setting values higher than 512kbps.
	Added checks for negative config values.
	Changed the config entries to the names of the variables. Should lower confusion.
	Added a config to disable the mod entirely.

1.0.1
	Fixed the log strings to have correct variable names.
	Added Github link.

1.0.0
	Initial commit.
```
