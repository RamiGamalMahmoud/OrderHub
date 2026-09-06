namespace OrderHub.Infrastructure.Services.WhatsApp;

internal static class WppScripts
{
    public static string SendTextMessage => """
    async ({ chatId, message }) => {
        try {
            if (!window.WPP ||
                !window.WPP.chat ||
                !window.WPP.chat.sendTextMessage) {
                return false;
            }

            if (window.WPP.chat.sendIsTyping) {
                await window.WPP.chat.sendIsTyping(chatId);
            }

            await new Promise(resolve =>
                setTimeout(
                    resolve,
                    3000 + Math.random() * 3000
                )
            );

            await window.WPP.chat.sendTextMessage(
                chatId,
                message
            );

            return true;
        }
        catch (error) {
            console.error(
                "WPP sendTextMessage error:",
                error
            );

            return false;
        }
    }
    """;

    public static string SendListMessage => """
    async ({ chatId, message }) => {
        try {
            if (!window.WPP ||
                !window.WPP.chat ||
                !window.WPP.chat.sendListMessage) {
                return false;
            }

            if (window.WPP.chat.sendIsTyping) {
                await window.WPP.chat.sendIsTyping(chatId);
            }

            await new Promise(resolve =>
                setTimeout(
                    resolve,
                    3000 + Math.random() * 3000
                )
            );

            await window.WPP.chat.sendListMessage(
                chatId,
                {
                    buttonText: "عرض الخيارات",

                    description: message,

                    title: "OrderHub",

                    footer: "اختر أحد الخيارات",

                    sections: [
                        {
                            title: "الطلبات",
                            rows: [
                                {
                                    rowId: "view_orders",
                                    title: "عرض الطلبات",
                                    description: "عرض جميع الطلبات"
                                },
                                {
                                    rowId: "pending_orders",
                                    title: "الطلبات المعلقة",
                                    description: "عرض الطلبات التي لم يتم تنفيذها"
                                }
                            ]
                        },
                        {
                            title: "الحساب",
                            rows: [
                                {
                                    rowId: "account",
                                    title: "بيانات الحساب",
                                    description: "عرض بيانات العميل"
                                },
                                {
                                    rowId: "contact_support",
                                    title: "التواصل مع الدعم",
                                    description: "التواصل مع خدمة العملاء"
                                }
                            ]
                        }
                    ]
                }
            );

            return true;
        }
        catch (error) {
            console.error(
                "WPP sendListMessage error:",
                error
            );

            return false;
        }
    }
    """;

    public static string GetGroutIdFromIntitationCode => """
        async (inviteCode) => {
            try {
                if (!window.WPP ||
                    !window.WPP.group ||
                    !window.WPP.group.getGroupInfoFromInviteCode) {
                    return null;
                }

                const groupInfo =
                    await window.WPP.group.getGroupInfoFromInviteCode(
                        inviteCode);

                if (!groupInfo || !groupInfo.id) {
                    return null;
                }

                return groupInfo.id._serialized || groupInfo.id;
            }
            catch (error) {
                console.error(
                    "WPP getGroupInfoFromInviteCode error:",
                    error
                );

                return null;
            }
        }
        """;

    public static string SendFileMessage => """
        async ({
            chatId,
            dataUri,
            caption,
            mediaType,
            fileName
        }) => {
            try {
                if (!window.WPP ||
                    !window.WPP.chat ||
                    !window.WPP.chat.sendFileMessage) {
                    return false;
                }

                const options = {
                    type: mediaType,
                    caption: caption || undefined
                };

                if (mediaType === "document") {
                    options.filename = fileName;
                }

                await window.WPP.chat.sendFileMessage(
                    chatId,
                    dataUri,
                    options
                );

                return true;
            }
            catch (error) {
                console.error(
                    "WPP sendFileMessage error:",
                    error
                );

                return false;
            }
        }
        """;

    public static string WaitForWhatsapReadyScript =>
        """
            () => {
                return document.querySelector(
                    '[data-testid="chat-list"]'
                ) ||
                document.querySelector(
                    '[data-testid="chatlist"]'
                ) ||
                document.querySelector(
                    '[data-testid="side"]'
                );
            }
            """;
}