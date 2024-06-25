import adsk.core
import traceback
import logging


def createTableInput(
    id: str,
    name: str,
    inputs: adsk.core.CommandInputs,
    columns: int,
    ratio: str,
    maxRows: int,
    minRows=1,
    columnSpacing=0,
    rowSpacing=0,
) -> adsk.core.TableCommandInput:
    try:
        input = inputs.addTableCommandInput(id, name, columns, ratio)
        input.minimumVisibleRows = minRows
        input.maximumVisibleRows = maxRows
        input.columnSpacing = columnSpacing
        input.rowSpacing = rowSpacing

        return input
    except BaseException:
        logging.getLogger(
            "{INTERNAL_ID}.UI.ConfigCommand.{self.__class__.__name__}.createTableInput()"
        ).error("Failed:\n{}".format(traceback.format_exc()))


def createBooleanInput(
    id: str,
    name: str,
    inputs: adsk.core.CommandInputs,
    tooltip="",
    tooltipadvanced="",
    checked=True,
    enabled=True,
    isCheckBox=True,
) -> adsk.core.BoolValueCommandInput:
    try:
        input = inputs.addBoolValueInput(id, name, isCheckBox)
        input.value = checked
        input.isEnabled = enabled
        input.tooltip = tooltip
        input.tooltipDescription = tooltipadvanced

        return input
    except BaseException:
        logging.getLogger(
            "{INTERNAL_ID}.UI.ConfigCommand.{self.__class__.__name__}.createBooleanInput()"
        ).error("Failed:\n{}".format(traceback.format_exc()))


def createTextBoxInput(
    id: str,
    name: str,
    inputs: adsk.core.CommandInputs,
    text: str,
    italics=True,
    bold=True,
    fontSize=10,
    alignment="center",
    rowCount=1,
    read=True,
    background="whitesmoke",
    tooltip="",
    advanced_tooltip="",
) -> adsk.core.TextBoxCommandInput:
    try:
        if bold:
            text = f"<b>{text}</b>"

        if italics:
            text = f"<i>{text}</i>"

        outputText = f"""<body style='background-color:{background};'>
            <div align='{alignment}'>
            <p style='font-size:{fontSize}px'>
            {text}
            </p>
            </body>
        """

        input = inputs.addTextBoxCommandInput(id, name, outputText, rowCount, read)
        input.tooltip = tooltip
        input.tooltipDescription = advanced_tooltip

        return input
    except BaseException:
        logging.getLogger("{INTERNAL_ID}.UI.ConfigCommand.createTextBoxInput()").error(
            "Failed:\n{}".format(traceback.format_exc())
        )
