import React, { ReactNode, useState } from "react"
import Label, { LabelSize } from "./Label"
import { Select as BaseSelect, SelectProps, SelectRootSlotProps } from "@mui/base/Select"
import { Option as BaseOption } from "@mui/base/Option"
import { styled } from "@mui/system"
import { Button } from "@mui/base/Button"
import UnfoldMoreRoundedIcon from "@mui/icons-material/UnfoldMoreRounded"

const Select = React.forwardRef(function Select<TValue extends NonNullable<unknown>, Multiple extends boolean>(
    props: SelectProps<TValue, Multiple>,
    ref: React.ForwardedRef<HTMLButtonElement>
) {
    const slots: SelectProps<TValue, Multiple>["slots"] = {
        root: CustomButton,
        listbox: Listbox,
        popup: Popup,
        ...props.slots,
    }

    return <BaseSelect {...props} ref={ref} slots={slots} slotProps={{ listbox: {}, popup: { disablePortal: true } }} />
}) as <TValue extends NonNullable<unknown>, Multiple extends boolean>(
    props: SelectProps<TValue, Multiple> & React.RefAttributes<HTMLButtonElement>
) => JSX.Element

type DropdownProps = {
    children?: ReactNode
    label?: string
    className?: string
    options: string[]
    onSelect: (opt: string) => void
}

const Dropdown: React.FC<DropdownProps> = ({ label, options, onSelect }) => {
    const [optionList, _setOptionList] = useState(options)

    return (
        <>
            {label && <Label size={LabelSize.Medium}>{label}</Label>}
            <div className="relative w-full">
                <Select
                    defaultValue={optionList[0]}
                    onChange={(
                        _event: React.MouseEvent | React.KeyboardEvent | React.FocusEvent | null,
                        value: string | unknown
                    ) => typeof value === "string" && onSelect && onSelect(value)}
                >
                    {optionList.map(option => (
                        <Option value={option} key={option}>
                            {option}
                        </Option>
                    ))}
                </Select>
            </div>
        </>
    )
}

const CustomButton = React.forwardRef(function CustomButton<
    TValue extends NonNullable<unknown>,
    Multiple extends boolean,
>(props: SelectRootSlotProps<TValue, Multiple>, ref: React.ForwardedRef<HTMLButtonElement>) {
    return (
        <StyledButton type="button" {...props} ref={ref}>
            {props.children}
            <UnfoldMoreRoundedIcon />
        </StyledButton>
    )
})

const StyledButton = styled(Button)`
    position: relative;
    text-align: left;
    width: 100%;
    background-image: linear-gradient(to right, var(--interactive-element-left), var(--interactive-element-right));
    border-radius: 0.375rem;
    border: none;
    outline: none;
    padding-left: calc(0.8em + 8px);

    &:hover,
    &:focus {
        outline: none;
    }

    & > svg {
        font-size: 1rem;
        position: absolute;
        height: 100%;
        top: 0;
        right: 10px;
    }
`

const Listbox = styled("ul")`
    box-sizing: border-box;
    width: 100%;
    background-image: linear-gradient(to right, var(--interactive-element-right), var(--interactive-element-left));
    border-radius: 1rem;
    padding: 8px;
`

const Option = styled(BaseOption)`
    list-style: none;
    cursor: default;
    padding: 0.6em 0.8em;
    border-radius: 1rem;
    cursor: pointer;
    &:hover {
        backdrop-filter: brightness(90%);
    }
    &:hover,
    &:focus {
        outline: none;
    }
`

const Popup = styled("div")`
    position: relative;
    z-index: 1;
    width: 100%;
`

export default Dropdown
