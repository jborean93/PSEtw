#include <Windows.h>
#include <tchar.h>
#include <evntrace.h>
#include <combaseapi.h>
#include <tdh.h>

int main()
{
    return sizeof(EVENT_RECORD);
}
