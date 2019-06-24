This project is no longer being actively developed or maintained. We recommend directly utilizing the [Poly API](https://developers.google.com/poly/develop/api) to access Poly assets from Unity, and using the existing Unity support for loading Poly compatible asset formats.

# Poly Toolkit for Unity

Copyright (c) 2017 Google Inc. All rights reserved.

This is the source code for Poly Toolkit, a plugin for the
[Unity](http://unity3d.com) engine that allows you to
import 3D assets from [Poly](https://poly.google.com) at
edit time and at runtime.

For more information, including setup instructions, refer to the [online
documentation](https://developers.google.com/poly/develop/unity).

_Unity is a trademark of Unity Technologies._

## Installing Poly Toolkit

If you are a _user_ of Poly Toolkit (most common case), you probably do not
need to download and build the source code. You can simply download a
[pre-built package](https://github.com/googlevr/poly-toolkit-unity/releases).

More instructions are available in the [online
documentation](https://developers.google.com/poly/develop/unity).

Note: If you are using Unity 2018 or above, you will need to enable the
_unsafe code_ option in **Player Settings** before installing the package.
This is because Poly Toolkit uses `unsafe {}` code blocks for direct
pointer manipulation when parsing files, for performance reasons.

## Building from Source

If the pre-built packages do not suit your needs (for example, you want the
cutting edge features that are checked in but not yet part of the official
releases), you can build from source. To do that:

* Install Unity 5.6.3 or later. Note: if you are not using the exact version of
  Unity that Poly Toolkit was developed with, you will get a warning to
  not check in any code, but you can ignore that, because you are just
  building it, not modifying the code.

* Clone this repo to your machine.

* Open it in Unity.

* Click **Poly > Dev > Build .unitypackage** on the menu.

* This will create a **unitypackage** file.

Then, simply use this package file to install Poly Toolkit in any project.

## License

For license information, see the `LICENSE` file.

