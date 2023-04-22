import React, { useEffect } from 'react';
import logo from './logo.svg';
import './App.scss';
import { useTelegram } from './useTelegram';
import {
  MainButton,
  useShowPopup,
  useThemeParams,
} from '@vkruglikov/react-telegram-web-app';
import { ConfigProvider, theme } from 'antd';
import MainButtonDemo from './MainButtonDemo';
import MyButton from './MyButton';
import { Route, Routes } from 'react-router-dom';
import SettingsPage from './pages/SettingsPage';

function App() {
  const { telegram } = useTelegram();

  useEffect(() => {
    telegram.ready();
  });

  const [colorScheme, themeParams] = useThemeParams();

  return (
    <div>
      <ConfigProvider
        theme={
          themeParams.text_color
            ? {
                algorithm:
                  colorScheme === 'dark'
                    ? theme.darkAlgorithm
                    : theme.defaultAlgorithm,
                token: {
                  colorText: themeParams.text_color,
                  colorPrimary: themeParams.button_color,
                  colorBgBase: themeParams.bg_color,
                },
              }
            : undefined
        }
      >
        <header className="App-header">
          <img src={logo} className="App-logo" alt="logo" />
        </header>
        <div className="contentWrapper">
          <Routes>
            <Route path="/" element={<MainButtonDemo />} />
            <Route path="/settings" element={<SettingsPage />} />
            {/* <BackButtonDemo /> */}
            {/* <ShowPopupDemo /> */}
            {/* <HapticFeedbackDemo /> */}
            {/* <ScanQrPopupDemo /> */}
          </Routes>
        </div>
      </ConfigProvider>
    </div>
  );
}

export default App;
