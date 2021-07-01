#pragma once

#include "Common.h"

namespace KlakSpout {

//
// Marshaling utility: std::set<std::string> -> IntPtr[]
// Returns an array of strings as IntPtr[].
//
// We prefer IntPtr[] over string[] because we don't know how the C# marshaler
// handles LPArray of LPStr... It's safe to marshal them manually.
//
// Although the marshaler will automatically free the array itself, the caller
// must free each string manually.
//
std::pair<char**, int> MarshalStringSet(const std::set<std::string>& source)
{
    // Output array
    auto array = static_cast<char**>
      (CoTaskMemAlloc(sizeof(char*) * source.size()));

    // Copy the source strings into the output array.
    auto i = 0u;
    for (const auto& s : source)
    {
        auto str = static_cast<char*>(CoTaskMemAlloc(s.size() + 1));
        s.copy(str, s.size());
        str[s.size()] = 0;
        array[i++] = str;
    }

    return std::make_pair(array, source.size());
}

} // namespace KlakSpout
