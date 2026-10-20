/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 Bulwark contributors.
 *
 * This file is part of Bulwark, a C# port of OpenXcom Extended.
 *
 * Bulwark is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Bulwark is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Bulwark.  If not, see <http://www.gnu.org/licenses/>.
 */

using Bulwark.Desktop;

using var host = new BulwarkHost(HostOptions.Parse(args));
host.Run();
