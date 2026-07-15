using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WuwaQuickSwapHelper.Models;

public enum StepState
{
    Waiting,   // 입력 대기
    Current,   // 현재 입력
    Success,   // 입력 성공
    Failed     // 입력 실패
}