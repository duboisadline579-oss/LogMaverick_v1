using System.Windows;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class TidTraceWindow : Window {
        // 생성자에서 string tid를 받도록 수정하여 빌드 에러 해결
        public TidTraceWindow(string tid) {
            InitializeComponent();
            this.Title = $"TID Trace Explorer - {tid}";
            
            // 데이터 컨텍스트 설정 (필요 시)
            if (this.DataContext is MainViewModel vm) {
                // 특정 TID만 필터링하는 로직 등을 추가할 수 있습니다.
            }
        }
    }
}
