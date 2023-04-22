import { Button, Form, Input, Typography, Switch } from 'antd';
import { FC, useState } from 'react';
import {
  MainButton,
  MainButtonProps,
  useShowPopup,
} from '@vkruglikov/react-telegram-web-app';
import { useTelegram } from './useTelegram';
import { useSearchParams } from 'react-router-dom';

const MyButton: FC = () => {
  const { telegram } = useTelegram();
  const [searchParams, setSearchParams] = useSearchParams();
  const tokenId = searchParams.get('tokenid');
  const {
    initDataUnsafe: { query_id },
  } = telegram;
  var jsonString = JSON.stringify(telegram.initDataUnsafe, null, 1);

  const showPopup = useShowPopup();
  const [buttonResult, setButtonResult] = useState('none');

  return (
    <>
      <Typography.Title level={3}>My Button</Typography.Title>
      <Button
        block
        type="primary"
        style={{ textAlign: 'left' }}
        onClick={async () => {
          const buttonId = await showPopup({
            title: 'My Title',
            message: 'My Message',
            buttons: [
              // {
              //   type: 'default',
              //   text: 'Default',
              // },
              {
                id: '--ok',
                type: 'ok',
              },
              {
                id: '--close',
                type: 'close',
              },
              // {
              //   type: 'cancel',
              // },
              {
                id: '--destructive',
                type: 'destructive',
                text: 'destructive',
              },
            ],
          });
          try {
            telegram.sendData('azaz');
          } catch (e: any) {
            showPopup({
              title: 'error',
              message: e,
            });
          }

          setButtonResult(buttonId);
          showPopup({
            title: 'Result',
            message: buttonId,
            buttons: [
              {
                type: 'close',
              },
            ],
          });
        }}
      >
        {jsonString.match(/.{1,4000}/g)?.map((x) => (
          <p>{x}</p>
        ))}
        <p>{tokenId}</p>
        {buttonResult}
      </Button>
    </>
  );
};
export default MyButton;
