import { Button, Form, Input, Typography, Switch } from 'antd';
import { FC, useState } from 'react';
import { useTelegram } from '../useTelegram';
import {
  MainButtonProps,
  MainButton,
  useShowPopup,
} from '@vkruglikov/react-telegram-web-app';
import { useSearchParams } from 'react-router-dom';

const SettingsPage: FC = () => {
  const { telegram } = useTelegram();
  const queryId = telegram.initDataUnsafe.query_id;
  const [searchParams, setSearchParams] = useSearchParams();
  const currentText = searchParams.get('currentText');
  const showPopup = useShowPopup();
  const [buttonState, setButtonState] = useState<{
    text: string;
  }>({
    text: currentText ?? '',
  });
  const onFinish = (values: any) => {
    try {
      telegram.sendData(
        JSON.stringify({ queryId, systemMessage: values.text }),
      );
      showPopup({
        title: 'Sent data',
        message: values.text,
      });
    } catch (e: any) {
      showPopup({
        title: 'error',
        message: e,
      });
    }
  };

  return (
    <>
      <Typography.Title level={3}>MainButton</Typography.Title>
      <Form
        labelCol={{ span: 6 }}
        name="basic"
        layout="horizontal"
        initialValues={buttonState}
        onFinish={onFinish}
        autoComplete="off"
      >
        <Form.Item label="Text" name="text">
          <Input
            multiple
            onChange={(event) =>
              setButtonState({
                ...buttonState,
                text: event.target.value,
              })
            }
          />
        </Form.Item>
      </Form>
      <div>
        <MainButton text="Save" onClick={() => onFinish(buttonState)} />
      </div>
    </>
  );
};
export default SettingsPage;
