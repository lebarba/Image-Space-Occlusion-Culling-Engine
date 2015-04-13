/** ******************************************************************************* **
 * GIGC - Grupo de Investigación de Gráficos por Computadora - UTN FRBA - Argentina
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *  **
 * <copyright> copyright (c) 2012 </copyright>	
 * <autor> Leandro Roberto Barbagallo </autor>
 * <license href="http://gigc.codeplex.com/license/">Ms-PL</license>
 ********************************************************************************** */


#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN


#if defined DLL_EXPORT
#define DllExport __declspec(dllexport)
#else
#define DllExport __declspec(dllimport)
#endif

#define ALIGNED16 __declspec( align( 16 ) )
#define EXTERN_C extern "C"

#include <cstdio>
#include <cstdlib>
#include <cassert>
#include <cfloat>

#include <vector>
#include <algorithm>
#include <Unknwn.h>

#include <omp.h>
#include <intrin.h>
#include <xmmintrin.h>
#include <pmmintrin.h>
#include <smmintrin.h>
