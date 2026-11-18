import * as Notifications from 'expo-notifications';
import { deIso } from './datas';
import type { Lembrete } from './tipos';

Notifications.setNotificationHandler({
  handleNotification: async () => ({ shouldShowBanner: true, shouldShowList: true, shouldPlaySound: true, shouldSetBadge: false }),
});

/** Reagenda um aviso local às 9h do dia de aviso de cada lembrete em aberto. */
export async function agendarAvisos(lembretes: Lembrete[]) {
  const { granted } = await Notifications.requestPermissionsAsync();
  if (!granted) return;
  await Notifications.cancelAllScheduledNotificationsAsync();
  for (const l of lembretes.filter((x) => !x.concluido)) {
    const quando = deIso(l.dataAviso);
    quando.setHours(9, 0, 0, 0);
    if (quando.getTime() <= Date.now()) continue;
    await Notifications.scheduleNotificationAsync({
      content: { title: `${l.titulo} vence amanhã`, body: 'Toque para ver os detalhes no SampaIA.' },
      trigger: { type: Notifications.SchedulableTriggerInputTypes.DATE, date: quando },
    });
  }
}
