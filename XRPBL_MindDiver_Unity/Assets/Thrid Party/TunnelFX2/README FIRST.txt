*****************************************
*               TUNNEL FX 2             *
*   (C) Copyright 2017-2022 Kronnect    *  
*              README FILE              *
*****************************************


How to use this asset
---------------------

Thanks for purchasing Tunnel FX 2.

Run the demo scene to get a first contact with the asset. It will allow you to change from one preset to another.

To add a tunnel to your scene:
- Click on top menu GameObject > 3D Object > TunnelFX2.

If you select your camera when adding the tunnel, it will be parented to that camera.
You can also position or change the scale of the tunnel. Default scale is 10x10x950.

Use the custom inspector to select a preset configuration or customize the effect. Some properties in the inspector shows a tooltip with some additional details.

Remember to remove the Demo folder once you don't need it anymore.


C# Scripting support
--------------------

Once you have a tunnel in your scene, you can access the script API from C#:

using TunnelEffect;

TunnelFX2 fx = TunnelFX2.instance;

fx.preset = TUNNEL_PRESET.xxx; (where xxx is one of the available presets defined in the enum TUNNEL_PRESET).

fx.globalAlpha = alpha; // changes transparency of tunnel (alpha = 0..1)

You can use alpha to make the tunnel invisible in the scene until you need it.


Support
-------

* Email support: contact@kronnect.com
* Website-Forum Support: https://kronnect.com/support
* Twitter: @Kronnect


Other Cool Assets!
------------------

Check our other assets on the Asset Store publisher page:
https://assetstore.unity.com/publishers/15018



Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Tunnel FX 2 will be eventually available on the Asset Store.



Credits
-------

- Tunnel FX 2 asset for Unity: (c) Kronnect - All Rights Reserved.
- Fire texture CC by Filter Forge: https://www.flickr.com/photos/filterforge/13908586495
- Cloud texture and others CC by webtreats: https://www.flickr.com/photos/webtreatsetc/5584892057


Version history
---------------

Current version
- Added "Capped Both Ends" option

Version 3.1
- Minimum Unity version required is now 2020.3.16
- Added "Aperture" option
- Material setter optimizations

Version 3.0 24/Feb/2022
- Added HDRP support

Version 2.2.1 30/Jan/2022
- Minor editor fixes and improvements

Version 2.2 29/Jul/2021
- Settings management optimizations

Version 2.1.2 3/Apr/2021
- Added support for Single Pass Instanced (VR) on URP

Version 2.1.1
- Memory optimizations
- Allowed animation when curved option

Version 2.1
- Improved tunnel rendering when curved + transparency options are enabled
- Added "Draw Behind All" option for transparent tunnels
- Added "UV Scale" option

Version 2.0
- Curved tunnel option

Version 1.4.3
- Improved inspector to make range selections more flexible

Version 1.4.2 - 17-DEC-2018
- [Fix] Fixed tunnel not showing up on creation when transparency is enabled

Version 1.4.1 - 15-NOV-2018
- Minor improvements

Version 1.4 - 5-MAR-2018
- Added collider support
- Added tint color
- Global transparency option on/off (off = uses opaque shader which is even faster)

Version 1.3 - 28-JAN-2018
- Added transparency to tunnel cap. Cap is now optional.
- Added FallOff Start and FallOff End to create a gradient which can be used to produce a vignette effect.

Version 1.2.1 - 13-NOV-2017
- Improved look of twist option

Version 1.2 - 15-AUG-2017
- Demo scene: added a initial alpha transition during start
- Tunnel's mesh renderer now is disabled if global alpha is set to 0. Also is enabled if global alpha is greater than 0.
- API: added CreateTunnel() method to programatically create tunnels using scripting

Version 1.1 - 15-JUL-2017
- [Fix] Fixed issue with tunnel mesh
- [Fix] Tunnel ends now blends properly according to global alpha setting

Version 1.0 - 8-JUN-2017






